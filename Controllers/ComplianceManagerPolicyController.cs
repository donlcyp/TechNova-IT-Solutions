using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    /// <summary>
    /// Handles all policy lifecycle operations for the Compliance Manager role.
    /// Admin keeps only Assign + View through AdminPolicyController.
    /// </summary>
    public class ComplianceManagerPolicyController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public ComplianceManagerPolicyController(
            IAdminService adminService,
            IEmailService emailService,
            IWebHostEnvironment environment,
            ApplicationDbContext context)
        {
            _adminService = adminService;
            _emailService = emailService;
            _environment = environment;
            _context = context;
        }

        // ── Auth helpers ────────────────────────────────────────

        /// <summary>
        /// Full policy lifecycle authority: create, update, delete, archive, restore.
        /// Only ChiefComplianceManager and SuperAdmin.
        /// </summary>
        /// <summary>
        /// Policy lifecycle (create/update/delete/archive/restore): all compliance and admin roles.
        /// Branch roles (Admin, ComplianceManager) create policies that go through CCM review.
        /// SuperAdmin and ChiefComplianceManager policies are auto-approved.
        /// </summary>
        private bool HasPolicyLifecycleAuthority()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            return role == RoleNames.ChiefComplianceManager || role == RoleNames.SuperAdmin
                || RoleNames.IsAdminRole(role) || role == RoleNames.ComplianceManager;
        }

        /// <summary>
        /// Policy assignment + view authority: all compliance and admin roles.
        /// </summary>
        private bool HasPolicyAssignmentAuthority()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            return role == RoleNames.ChiefComplianceManager || role == RoleNames.ComplianceManager || RoleNames.IsAdminRole(role) || role == RoleNames.SuperAdmin;
        }

        private int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString(SessionKeys.UserId);
            return int.TryParse(s, out var id) ? id : null;
        }

        private int? GetCallerBranchId()
        {
            var s = HttpContext.Session.GetString(SessionKeys.BranchId);
            return int.TryParse(s, out var id) ? id : null;
        }

        /// <summary>
        /// SuperAdmin and ChiefComplianceManager have company-wide (unscoped) access.
        /// </summary>
        private bool HasGlobalScope()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            return role == RoleNames.SuperAdmin || role == RoleNames.ChiefComplianceManager;
        }

        private bool IsSuperAdmin()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            return role == RoleNames.SuperAdmin;
        }

        private string? GetCallerRole()
        {
            return HttpContext.Session.GetString(SessionKeys.UserRole);
        }

        // ── Email helpers ───────────────────────────────────────

        private sealed class EmailNotificationSummary
        {
            public int Recipients { get; set; }
            public int SentCount { get; set; }
            public int FailedCount { get; set; }
            public List<string> FailedRecipients { get; set; } = new();
        }

        private async Task<EmailNotificationSummary> NotifyEmployeesPolicyAssignedAsync(
            IEnumerable<int> policyIds, IEnumerable<int> employeeIds)
        {
            var summary = new EmailNotificationSummary();
            var pidList = policyIds.Distinct().ToList();
            var eidList = employeeIds.Distinct().ToList();
            if (!pidList.Any() || !eidList.Any()) return summary;

            var employees = await _context.Users
                .Where(u => eidList.Contains(u.UserId) &&
                            u.Role == RoleNames.Employee &&
                            !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            var policies = await _context.Policies
                .Where(p => pidList.Contains(p.PolicyId))
                .Select(p => new { p.PolicyId, p.PolicyTitle })
                .ToListAsync();

            var policyText = string.Join(", ", policies.Select(p => p.PolicyTitle));
            summary.Recipients = employees.Count;

            foreach (var emp in employees)
            {
                var body = $@"
                    <h2>New Policy Assigned</h2>
                    <p>Hello {emp.FirstName},</p>
                    <p>The following policy/policies were assigned to you:</p>
                    <p><strong>{policyText}</strong></p>
                    <p>Please log in and acknowledge them.</p>";
                var result = await _emailService.SendEmailAsync(emp.Email!, "New Policy Assignment", body);
                if (result.Success) summary.SentCount++;
                else { summary.FailedCount++; summary.FailedRecipients.Add(emp.Email!); }
            }

            return summary;
        }

        private async Task<EmailNotificationSummary> NotifyEmployeesPolicyUpdatedAsync(int policyId, string title)
        {
            var summary = new EmailNotificationSummary();
            var eids = await _context.PolicyAssignments
                .Where(pa => pa.PolicyId == policyId)
                .Select(pa => pa.UserId).Distinct().ToListAsync();
            if (!eids.Any()) return summary;

            var employees = await _context.Users
                .Where(u => eids.Contains(u.UserId) && u.Role == RoleNames.Employee && !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            summary.Recipients = employees.Count;
            foreach (var emp in employees)
            {
                var body = $@"
                    <h2>Policy Update Notice</h2>
                    <p>Hello {emp.FirstName},</p>
                    <p>Your assigned policy <strong>{title}</strong> has been updated. Please review it.</p>";
                var result = await _emailService.SendEmailAsync(emp.Email!, "Assigned Policy Updated", body);
                if (result.Success) summary.SentCount++;
                else { summary.FailedCount++; summary.FailedRecipients.Add(emp.Email!); }
            }

            return summary;
        }

        // ── Policy CRUD ─────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> CreatePolicy([FromBody] PolicyData policyData)
        {
            if (!HasPolicyLifecycleAuthority()) return Unauthorized(new { success = false, message = "Access denied. Only Chief Compliance Manager or Super Admin can create policies." });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            policyData.UploadedDate ??= DateTime.Now;
            policyData.CallerUserId = GetCurrentUserId();
            policyData.CallerRole = GetCallerRole();

            // Branch Admins/CMs automatically stamp their branch; SuperAdmin/CCM keeps null (company-wide)
            if (!HasGlobalScope())
            {
                policyData.BranchId = GetCallerBranchId();
            }

            var result = await _adminService.CreatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created policy: {policyData.PolicyTitle}", "Policy");
                var msg = policyData.CallerRole is RoleNames.BranchAdmin or RoleNames.ComplianceManager
                    ? "Policy created and submitted for CCM review."
                    : "Policy created successfully.";
                return Ok(new { success = true, message = msg });
            }

            return BadRequest(new { success = false, message = "Failed to create policy" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePolicy([FromBody] PolicyData policyData)
        {
            if (!HasPolicyLifecycleAuthority()) return Unauthorized(new { success = false, message = "Access denied. Only Chief Compliance Manager or Super Admin can update policies." });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            policyData.CallerUserId = GetCurrentUserId();
            policyData.CallerRole = GetCallerRole();

            var result = await _adminService.UpdatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated policy: {policyData.PolicyTitle}", "Policy");

                var emailSummary = new EmailNotificationSummary();
                bool directUpdate = policyData.CallerRole is not (RoleNames.BranchAdmin or RoleNames.ComplianceManager);
                if (directUpdate && policyData.PolicyId > 0)
                    emailSummary = await NotifyEmployeesPolicyUpdatedAsync(policyData.PolicyId, policyData.PolicyTitle);

                var msg = directUpdate
                    ? $"Policy updated. Email sent: {emailSummary.SentCount}, failed: {emailSummary.FailedCount}."
                    : "Policy update submitted for CCM review.";

                return Ok(new
                {
                    success = true,
                    message = msg,
                    emailRecipients = emailSummary.Recipients,
                    emailSent = emailSummary.SentCount,
                    emailFailed = emailSummary.FailedCount,
                    failedRecipients = emailSummary.FailedRecipients
                });
            }

            return BadRequest(new { success = false, message = "Failed to update policy" });
        }

        [HttpPost]
        public async Task<IActionResult> UploadPolicy(
            IFormFile? policyFile,
            [FromForm] string policyTitle,
            [FromForm] string category,
            [FromForm] string description,
            [FromForm] string status,
            [FromForm] int? policyId)
        {
            if (!HasPolicyLifecycleAuthority()) return Unauthorized(new { success = false, message = "Access denied. Only Chief Compliance Manager or Super Admin can upload policies." });
            if (string.IsNullOrWhiteSpace(policyTitle)) return BadRequest(new { success = false, message = "Policy title is required" });

            string filePath = string.Empty;
            if (policyFile != null && policyFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "policies");
                Directory.CreateDirectory(uploadsDir);
                var safeFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(policyFile.FileName)}";
                var fullPath = Path.Combine(uploadsDir, safeFileName);
                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await policyFile.CopyToAsync(stream);
                }
                filePath = $"/uploads/policies/{safeFileName}";
            }

            var data = new PolicyData
            {
                PolicyTitle = policyTitle,
                Category = category,
                Description = description,
                FilePath = filePath,
                UploadedDate = DateTime.Now,
                CallerUserId = GetCurrentUserId(),
                CallerRole = GetCallerRole()
            };

            // Branch Admins/CMs stamp their branch; SuperAdmin/CCM keeps null (company-wide)
            if (!HasGlobalScope())
            {
                data.BranchId = GetCallerBranchId();
            }

            bool ok;
            if (policyId.HasValue && policyId.Value > 0)
            {
                data.PolicyId = policyId.Value;
                if (string.IsNullOrEmpty(filePath)) data.FilePath = null!;
                ok = await _adminService.UpdatePolicyAsync(data);
                if (ok) await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated policy with file: {policyTitle}", "Policy");
            }
            else
            {
                ok = await _adminService.CreatePolicyAsync(data);
                if (ok) await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created policy with file: {policyTitle}", "Policy");
            }

            if (ok)
            {
                var emailSummary = new EmailNotificationSummary();
                if (policyId.HasValue && policyId.Value > 0)
                    emailSummary = await NotifyEmployeesPolicyUpdatedAsync(data.PolicyId, policyTitle);

                return Ok(new
                {
                    success = true,
                    message = $"Policy saved. Email sent: {emailSummary.SentCount}, failed: {emailSummary.FailedCount}.",
                    emailRecipients = emailSummary.Recipients,
                    emailSent = emailSummary.SentCount,
                    emailFailed = emailSummary.FailedCount,
                    failedRecipients = emailSummary.FailedRecipients
                });
            }

            return BadRequest(new { success = false, message = "Failed to save policy" });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePolicy(int policyId)
        {
            if (!HasPolicyLifecycleAuthority()) return Unauthorized(new { success = false, message = "Access denied. Only Chief Compliance Manager or Super Admin can delete policies." });
            var result = await _adminService.DeletePolicyAsync(policyId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted policy ID: {policyId}", "Policy");
                return Ok(new { success = true, message = "Policy deleted successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to delete policy" });
        }

        [HttpPost]
        public async Task<IActionResult> ArchivePolicy(int policyId)
        {
            if (!HasPolicyLifecycleAuthority()) return Unauthorized(new { success = false, message = "Access denied. Only Chief Compliance Manager or Super Admin can archive policies." });
            var result = await _adminService.ArchivePolicyAsync(policyId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Archived policy ID: {policyId}", "Policy");
                return Ok(new { success = true, message = "Policy archived successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to archive policy" });
        }

        [HttpPost]
        public async Task<IActionResult> RestorePolicy(int policyId)
        {
            if (!HasPolicyLifecycleAuthority()) return Unauthorized(new { success = false, message = "Access denied. Only Chief Compliance Manager or Super Admin can restore policies." });
            var result = await _adminService.RestorePolicyAsync(policyId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Restored policy ID: {policyId}", "Policy");
                return Ok(new { success = true, message = "Policy restored successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to restore policy" });
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicyDetail(int policyId)
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized(new { success = false, message = "Not authenticated" });

            var detail = await _adminService.GetPolicyDetailAsync(policyId);
            if (detail == null) return NotFound(new { success = false, message = "Policy not found" });

            return Ok(new
            {
                success = true,
                policy = new
                {
                    detail.PolicyId,
                    detail.PolicyTitle,
                    detail.Category,
                    detail.Description,
                    detail.FilePath,
                    DateUploaded = detail.DateUploaded?.ToString("MMM dd, yyyy"),
                    detail.UploadedBy,
                    detail.IsArchived,
                    ArchivedDate = detail.ArchivedDate?.ToString("MMM dd, yyyy"),
                    detail.AssignedEmployees,
                    detail.AssignedSuppliers
                }
            });
        }

        // ── Policy Assignment (CM can also assign) ──────────────

        [HttpPost]
        public async Task<IActionResult> AssignPolicy([FromBody] PolicyAssignmentRequest request)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
            if (request == null) return BadRequest(new { success = false, message = "Invalid request" });

            var policyIds = request.PolicyIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            if (!policyIds.Any() && request.PolicyId > 0) policyIds.Add(request.PolicyId);
            if (!policyIds.Any()) return BadRequest(new { success = false, message = "At least one policy must be selected." });

            if (!HasGlobalScope())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue)
                {
                    if (request.EmployeeIds.Any())
                    {
                        var outOfBranch = await _context.Users
                            .Where(u => request.EmployeeIds.Contains(u.UserId) && u.BranchId != callerBranchId)
                            .AnyAsync();
                        if (outOfBranch) return BadRequest(new { success = false, message = "You can only assign policies to employees within your branch." });
                    }

                    if (request.SupplierIds.Any())
                    {
                        var invalid = await _context.Suppliers
                            .Where(s => request.SupplierIds.Contains(s.SupplierId) && s.BranchId != callerBranchId && s.BranchId != null)
                            .AnyAsync();
                        if (invalid) return BadRequest(new { success = false, message = "You can only assign policies to your branch's suppliers or global suppliers." });
                    }
                }
            }

            var success = true;
            foreach (var pid in policyIds)
            {
                if (request.EmployeeIds.Any()) success &= await _adminService.AssignPolicyToEmployeesAsync(pid, request.EmployeeIds);
                if (request.SupplierIds.Any()) success &= await _adminService.AssignPolicyToSuppliersAsync(pid, request.SupplierIds);
            }

            if (success)
            {
                await _adminService.LogActivityAsync(
                    GetCurrentUserId(),
                    $"Assigned {policyIds.Count} policy(ies) to {request.EmployeeIds.Count} employee(s) and {request.SupplierIds.Count} supplier(s)",
                    "Policy");

                var emailSummary = new EmailNotificationSummary();
                if (request.EmployeeIds.Any())
                    emailSummary = await NotifyEmployeesPolicyAssignedAsync(policyIds, request.EmployeeIds);

                return Ok(new
                {
                    success = true,
                    message = $"Policy assignment completed. Email sent: {emailSummary.SentCount}, failed: {emailSummary.FailedCount}.",
                    emailRecipients = emailSummary.Recipients,
                    emailSent = emailSummary.SentCount,
                    emailFailed = emailSummary.FailedCount,
                    failedRecipients = emailSummary.FailedRecipients
                });
            }

            return BadRequest(new { success = false, message = "Failed to assign policy" });
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicyAssignmentStatus(int policyId)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyId <= 0) return BadRequest(new { success = false, message = "Invalid policy id" });

            var status = await _adminService.GetPolicyAssignmentStatusAsync(policyId);
            return Ok(new
            {
                success = true,
                assignedEmployeeIds = status.AssignedEmployeeIds,
                assignedSupplierIds = status.AssignedSupplierIds
            });
        }

        // ── Supplier compliance actions ─────────────────────────

        [HttpPost]
        public async Task<IActionResult> SuspendSupplier(int supplierId, [FromBody] SupplierSuspendRequest? request)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return NotFound(new { success = false, message = "Supplier not found" });

            supplier.Status = "Suspended";
            supplier.TerminationReason = request?.Reason ?? "Suspended for non-compliance by Compliance Manager";
            supplier.TerminatedAt = DateTime.UtcNow;
            supplier.TerminatedByUserId = GetCurrentUserId();
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(), $"Suspended supplier: {supplier.SupplierName} — {supplier.TerminationReason}", "Compliance");

            // ── Email notification ────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(supplier.Email))
            {
                var contact = !string.IsNullOrWhiteSpace(supplier.ContactPersonFirstName)
                    ? $"{supplier.ContactPersonFirstName} {supplier.ContactPersonLastName}".Trim()
                    : supplier.SupplierName;
                var reason = supplier.TerminationReason;
                var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                var body = $"Dear {contact},\n\nYour supplier account for {supplier.SupplierName} has been suspended due to non-compliance with TechNova IT Solutions policies.\n\nReason: {reason}\nSuspended On: {nowStr} UTC\n\nTo resolve this suspension, please contact the TechNova compliance team as soon as possible.\n\n— TechNova IT Solutions Compliance Team";
                try { await _emailService.SendEmailAsync(supplier.Email!, $"[TechNova] Supplier Account Suspended – {supplier.SupplierName}", body); } catch { }
            }

            return Ok(new { success = true, message = $"Supplier '{supplier.SupplierName}' has been suspended." });
        }

        [HttpPost]
        public async Task<IActionResult> UnsuspendSupplier(int supplierId)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return NotFound(new { success = false, message = "Supplier not found" });

            supplier.Status = "Active";
            supplier.TerminationReason = null;
            supplier.TerminatedAt = null;
            supplier.TerminatedByUserId = null;
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(), $"Re-activated supplier: {supplier.SupplierName}", "Compliance");

            // ── Email notification ────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(supplier.Email))
            {
                var contact = !string.IsNullOrWhiteSpace(supplier.ContactPersonFirstName)
                    ? $"{supplier.ContactPersonFirstName} {supplier.ContactPersonLastName}".Trim()
                    : supplier.SupplierName;
                var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                var body = $"Dear {contact},\n\nYour supplier account for {supplier.SupplierName} has been reactivated. You now have full access to the TechNova IT Solutions supplier portal.\n\nReactivated On: {nowStr} UTC\n\nIf you have any questions, please contact the TechNova compliance team.\n\n— TechNova IT Solutions Compliance Team";
                try { await _emailService.SendEmailAsync(supplier.Email!, $"[TechNova] Supplier Account Reactivated – {supplier.SupplierName}", body); } catch { }
            }

            return Ok(new { success = true, message = $"Supplier '{supplier.SupplierName}' has been re-activated." });
        }

        // ── Violation management endpoints ──────────────────────

        // ── Employee compliance actions ──────────────────────────

        [HttpPost]
        public async Task<IActionResult> SuspendEmployee(int userId, [FromBody] EmployeeSuspendRequest? request)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "Employee not found" });

            user.Status = "Suspended";
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Suspended employee: {user.FirstName} {user.LastName} — {request?.Reason ?? "Non-compliance"}", "Compliance");

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var name = $"{user.FirstName} {user.LastName}";
                var reason = request?.Reason ?? "Non-compliance with company policies";
                var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                var body = $"Dear {name},\n\nYour TechNova IT Solutions employee account has been suspended.\n\nReason: {reason}\nSuspended On: {nowStr} UTC\n\nPlease contact your Compliance Manager or HR department to resolve this matter.\n\n— TechNova IT Solutions Compliance Team";
                try { await _emailService.SendEmailAsync(user.Email, "[TechNova] Your Account Has Been Suspended", body); } catch { }
            }

            return Ok(new { success = true, message = $"Employee '{user.FirstName} {user.LastName}' has been suspended." });
        }

        [HttpPost]
        public async Task<IActionResult> UnsuspendEmployee(int userId)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "Employee not found" });

            user.Status = "Active";
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Re-activated employee: {user.FirstName} {user.LastName}", "Compliance");

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var name = $"{user.FirstName} {user.LastName}";
                var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                var body = $"Dear {name},\n\nYour TechNova IT Solutions employee account has been reactivated. You now have full access to the portal.\n\nReactivated On: {nowStr} UTC\n\nIf you have any questions, please contact the compliance team.\n\n— TechNova IT Solutions Compliance Team";
                try { await _emailService.SendEmailAsync(user.Email, "[TechNova] Your Account Has Been Reactivated", body); } catch { }
            }

            return Ok(new { success = true, message = $"Employee '{user.FirstName} {user.LastName}' has been reactivated." });
        }

        [HttpGet]
        public async Task<IActionResult> GetViolations(string? type, string? status)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            var query = _context.ComplianceViolations
                .Include(v => v.PolicyAssignment).ThenInclude(pa => pa!.User)
                .Include(v => v.PolicyAssignment).ThenInclude(pa => pa!.Policy)
                .Include(v => v.SupplierPolicy).ThenInclude(sp => sp!.Supplier)
                .Include(v => v.SupplierPolicy).ThenInclude(sp => sp!.Policy)
                .Include(v => v.RaisedByUser)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(type) && type != "All")
                query = query.Where(v => v.ViolationType == type);

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(v => v.Status == status);

            // Branch scoping
            if (scoped)
            {
                query = query.Where(v =>
                    (v.PolicyAssignment != null && v.PolicyAssignment.User != null && v.PolicyAssignment.User.BranchId == callerBranchId) ||
                    (v.SupplierPolicy != null && v.SupplierPolicy.Supplier != null && (v.SupplierPolicy.Supplier.BranchId == callerBranchId || v.SupplierPolicy.Supplier.BranchId == null)));
            }

            var violations = await query
                .OrderByDescending(v => v.RaisedDate)
                .Take(200)
                .Select(v => new
                {
                    v.ViolationId,
                    v.ViolationType,
                    v.Status,
                    v.Description,
                    v.Notes,
                    v.Resolution,
                    RaisedDate = v.RaisedDate.ToString("yyyy-MM-dd HH:mm"),
                    ResolvedDate = v.ResolvedDate.HasValue ? v.ResolvedDate.Value.ToString("yyyy-MM-dd HH:mm") : null,
                    RaisedBy = v.RaisedByUser != null ? $"{v.RaisedByUser.FirstName} {v.RaisedByUser.LastName}" : "System",
                    SubjectName = v.ViolationType == "Employee"
                        ? (v.PolicyAssignment != null && v.PolicyAssignment.User != null
                            ? $"{v.PolicyAssignment.User.FirstName} {v.PolicyAssignment.User.LastName}" : "Unknown")
                        : (v.SupplierPolicy != null && v.SupplierPolicy.Supplier != null
                            ? v.SupplierPolicy.Supplier.SupplierName : "Unknown"),
                    PolicyName = v.ViolationType == "Employee"
                        ? (v.PolicyAssignment != null && v.PolicyAssignment.Policy != null
                            ? v.PolicyAssignment.Policy.PolicyTitle : "Unknown")
                        : (v.SupplierPolicy != null && v.SupplierPolicy.Policy != null
                            ? v.SupplierPolicy.Policy.PolicyTitle : "Unknown")
                })
                .ToListAsync();

            return Ok(new { success = true, violations });
        }

        [HttpGet]
        public async Task<IActionResult> GetArchivedPolicies(string? search)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            var query = _context.Policies
                .Where(p => p.IsArchived)
                .AsNoTracking().AsQueryable();

            if (scoped)
                query = query.Where(p => p.BranchId == callerBranchId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.PolicyTitle.ToLower().Contains(s) ||
                    (p.Description != null && p.Description.ToLower().Contains(s)) ||
                    (p.Category    != null && p.Category.ToLower().Contains(s)));
            }

            var policies = await query.OrderByDescending(p => p.ArchivedDate)
                .Select(p => new {
                    p.PolicyId,
                    p.PolicyTitle,
                    p.Category,
                    p.Description,
                    ArchivedDate = p.ArchivedDate.HasValue ? p.ArchivedDate.Value.ToString("yyyy-MM-dd") : null,
                    DateUploaded = p.DateUploaded.HasValue ? p.DateUploaded.Value.ToString("yyyy-MM-dd") : null
                }).ToListAsync();

            return Ok(new { success = true, count = policies.Count, policies });
        }

        [HttpGet]
        public async Task<IActionResult> GetBranchArchive(string? from, string? to, string? search)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            DateTime? fromDate = DateTime.TryParse(from, out var fd) ? fd.Date : null;
            DateTime? toDate   = DateTime.TryParse(to,   out var td) ? td.Date.AddDays(1).AddTicks(-1) : null;

            // ── Resolved Violations ───────────────────────────────────
            var violQuery = _context.ComplianceViolations
                .Include(v => v.PolicyAssignment).ThenInclude(pa => pa!.User)
                .Include(v => v.PolicyAssignment).ThenInclude(pa => pa!.Policy)
                .Include(v => v.SupplierPolicy).ThenInclude(sp => sp!.Supplier)
                .Include(v => v.SupplierPolicy).ThenInclude(sp => sp!.Policy)
                .Include(v => v.RaisedByUser)
                .Where(v => v.Status == "Resolved")
                .AsNoTracking().AsQueryable();

            if (scoped)
                violQuery = violQuery.Where(v =>
                    (v.PolicyAssignment != null && v.PolicyAssignment.User != null && v.PolicyAssignment.User.BranchId == callerBranchId) ||
                    (v.SupplierPolicy != null && v.SupplierPolicy.Supplier != null && (v.SupplierPolicy.Supplier.BranchId == callerBranchId || v.SupplierPolicy.Supplier.BranchId == null)));

            if (fromDate.HasValue) violQuery = violQuery.Where(v => v.ResolvedDate >= fromDate);
            if (toDate.HasValue)   violQuery = violQuery.Where(v => v.ResolvedDate <= toDate);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                violQuery = violQuery.Where(v =>
                    (v.Description != null && v.Description.ToLower().Contains(s)) ||
                    (v.Resolution  != null && v.Resolution.ToLower().Contains(s)) ||
                    (v.PolicyAssignment != null && v.PolicyAssignment.User != null &&
                        (v.PolicyAssignment.User.FirstName + " " + v.PolicyAssignment.User.LastName).ToLower().Contains(s)) ||
                    (v.SupplierPolicy != null && v.SupplierPolicy.Supplier != null &&
                        v.SupplierPolicy.Supplier.SupplierName.ToLower().Contains(s)));
            }

            var violations = await violQuery.OrderByDescending(v => v.ResolvedDate).Take(300)
                .Select(v => new {
                    v.ViolationId, v.ViolationType, v.Description, v.Resolution,
                    RaisedDate    = v.RaisedDate.ToString("yyyy-MM-dd"),
                    ResolvedDate  = v.ResolvedDate.HasValue ? v.ResolvedDate.Value.ToString("yyyy-MM-dd") : null,
                    RaisedBy      = v.RaisedByUser != null ? v.RaisedByUser.FirstName + " " + v.RaisedByUser.LastName : "System",
                    SubjectName   = v.ViolationType == "Employee"
                        ? (v.PolicyAssignment != null && v.PolicyAssignment.User != null ? v.PolicyAssignment.User.FirstName + " " + v.PolicyAssignment.User.LastName : "Unknown")
                        : (v.SupplierPolicy  != null && v.SupplierPolicy.Supplier != null ? v.SupplierPolicy.Supplier.SupplierName : "Unknown"),
                    PolicyName    = v.ViolationType == "Employee"
                        ? (v.PolicyAssignment != null && v.PolicyAssignment.Policy != null ? v.PolicyAssignment.Policy.PolicyTitle : "Unknown")
                        : (v.SupplierPolicy  != null && v.SupplierPolicy.Policy   != null ? v.SupplierPolicy.Policy.PolicyTitle   : "Unknown")
                }).ToListAsync();

            // ── Acknowledged Policy Assignments (Employees) ───────────
            var ackQuery = _context.ComplianceStatuses
                .Include(cs => cs.PolicyAssignment).ThenInclude(pa => pa.User)
                .Include(cs => cs.PolicyAssignment).ThenInclude(pa => pa.Policy)
                .Where(cs => cs.Status == "Acknowledged" && cs.AcknowledgedDate != null)
                .AsNoTracking().AsQueryable();

            if (scoped)
                ackQuery = ackQuery.Where(cs => cs.PolicyAssignment.User.BranchId == callerBranchId);

            if (fromDate.HasValue) ackQuery = ackQuery.Where(cs => cs.AcknowledgedDate >= fromDate);
            if (toDate.HasValue)   ackQuery = ackQuery.Where(cs => cs.AcknowledgedDate <= toDate);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                ackQuery = ackQuery.Where(cs =>
                    (cs.PolicyAssignment.User.FirstName + " " + cs.PolicyAssignment.User.LastName).ToLower().Contains(s) ||
                    cs.PolicyAssignment.Policy.PolicyTitle.ToLower().Contains(s));
            }

            var acknowledgements = await ackQuery.OrderByDescending(cs => cs.AcknowledgedDate).Take(300)
                .Select(cs => new {
                    cs.PolicyAssignment.AssignmentId,
                    EmployeeName     = cs.PolicyAssignment.User.FirstName + " " + cs.PolicyAssignment.User.LastName,
                    PolicyTitle      = cs.PolicyAssignment.Policy.PolicyTitle,
                    AssignedDate     = cs.PolicyAssignment.AssignedDate.HasValue ? cs.PolicyAssignment.AssignedDate.Value.ToString("yyyy-MM-dd") : null,
                    AcknowledgedDate = cs.AcknowledgedDate!.Value.ToString("yyyy-MM-dd")
                }).ToListAsync();

            return Ok(new {
                success = true,
                resolvedCount      = violations.Count,
                acknowledgedCount  = acknowledgements.Count,
                violations,
                acknowledgements
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateViolation([FromBody] CreateViolationRequest request)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
            if (request == null) return BadRequest(new { success = false, message = "Invalid request" });

            var now = DateTime.UtcNow;
            var nowStr = now.ToString("yyyy-MM-dd HH:mm");

            // Initial timeline entry always written to Notes
            var timelineEntry = $"[TIMELINE:Open:{nowStr}] Violation raised";
            var initialNotes = timelineEntry;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                initialNotes += $"\n[{nowStr}] {request.Notes}";

            var violation = new Models.ComplianceViolation
            {
                ViolationType = request.ViolationType ?? "Employee",
                PolicyAssignmentId = request.PolicyAssignmentId,
                SupplierPolicyId = request.SupplierPolicyId,
                Description = request.Description,
                Notes = initialNotes,
                Status = "Open",
                RaisedDate = now,
                RaisedByUserId = GetCurrentUserId()
            };

            _context.ComplianceViolations.Add(violation);
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Raised compliance violation #{violation.ViolationId}: {request.Description}", "Compliance");

            // ── Email notification ────────────────────────────────────
            try
            {
                if (violation.ViolationType == "Employee" && violation.PolicyAssignmentId.HasValue)
                {
                    var pa = await _context.PolicyAssignments
                        .Include(x => x.User)
                        .Include(x => x.Policy)
                        .FirstOrDefaultAsync(x => x.AssignmentId == violation.PolicyAssignmentId.Value);
                    if (pa?.User != null && !string.IsNullOrWhiteSpace(pa.User.Email))
                    {
                        var name = $"{pa.User.FirstName} {pa.User.LastName}";
                        var policy = pa.Policy?.PolicyTitle ?? "N/A";
                        var body = $"Dear {name},\n\nA compliance violation has been raised against one of your assigned policies.\n\nViolation #: {violation.ViolationId}\nPolicy: {policy}\nDescription: {request.Description ?? "N/A"}\nRaised On: {nowStr} UTC\n\nPlease contact your Compliance Manager immediately for further information and corrective action.\n\n— TechNova IT Solutions Compliance Team";
                        await _emailService.SendEmailAsync(pa.User.Email,
                            $"[TechNova] Compliance Violation #{violation.ViolationId} Raised – Action Required", body);
                    }
                }
                else if (violation.ViolationType == "Supplier" && violation.SupplierPolicyId.HasValue)
                {
                    var sp = await _context.SupplierPolicies
                        .Include(x => x.Supplier)
                        .Include(x => x.Policy)
                        .FirstOrDefaultAsync(x => x.SupplierPolicyId == violation.SupplierPolicyId.Value);
                    if (sp?.Supplier != null && !string.IsNullOrWhiteSpace(sp.Supplier.Email))
                    {
                        var contact = !string.IsNullOrWhiteSpace(sp.Supplier.ContactPersonFirstName)
                            ? $"{sp.Supplier.ContactPersonFirstName} {sp.Supplier.ContactPersonLastName}".Trim()
                            : sp.Supplier.SupplierName;
                        var policy = sp.Policy?.PolicyTitle ?? "N/A";
                        var body = $"Dear {contact},\n\nA compliance violation has been recorded for {sp.Supplier.SupplierName} against a TechNova IT Solutions policy.\n\nViolation #: {violation.ViolationId}\nPolicy: {policy}\nDescription: {request.Description ?? "N/A"}\nRaised On: {nowStr} UTC\n\nPlease contact your assigned compliance officer to address this matter promptly.\n\n— TechNova IT Solutions Compliance Team";
                        await _emailService.SendEmailAsync(sp.Supplier.Email,
                            $"[TechNova] Compliance Violation #{violation.ViolationId} Notice – {sp.Supplier.SupplierName}", body);
                    }
                }
            }
            catch { /* email failure is non-blocking */ }

            return Ok(new { success = true, message = "Violation created successfully", violationId = violation.ViolationId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateViolationStatus(int violationId, [FromBody] UpdateViolationStatusRequest request)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var violation = await _context.ComplianceViolations
                .Include(v => v.PolicyAssignment).ThenInclude(pa => pa!.User)
                .Include(v => v.PolicyAssignment).ThenInclude(pa => pa!.Policy)
                .Include(v => v.SupplierPolicy).ThenInclude(sp => sp!.Supplier)
                .Include(v => v.SupplierPolicy).ThenInclude(sp => sp!.Policy)
                .FirstOrDefaultAsync(v => v.ViolationId == violationId);
            if (violation == null) return NotFound(new { success = false, message = "Violation not found" });

            var oldStatus = violation.Status;
            var newStatus = request.Status ?? violation.Status;
            var now = DateTime.UtcNow;
            var nowStr = now.ToString("yyyy-MM-dd HH:mm");

            violation.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(request.Resolution))
                violation.Resolution = request.Resolution;
            if (request.Status == "Resolved")
                violation.ResolvedDate = now;

            // Always write a timeline entry for the status change
            var statusLabel = newStatus == "UnderReview" ? "Under Review" : newStatus;
            var timelineEntry = $"[TIMELINE:{newStatus}:{nowStr}] Status updated to {statusLabel}";
            var notesBuffer = (violation.Notes ?? "").TrimEnd();
            notesBuffer += (notesBuffer.Length > 0 ? "\n" : "") + timelineEntry;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                notesBuffer += $"\n[{nowStr}] {request.Notes}";
            violation.Notes = notesBuffer;

            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Updated violation #{violationId} → {violation.Status}", "Compliance");

            // ── Email notification ────────────────────────────────────
            try
            {
                string? recipientEmail = null, recipientName = null, policyTitle = null;
                if (violation.ViolationType == "Employee" && violation.PolicyAssignment?.User != null)
                {
                    recipientEmail = violation.PolicyAssignment.User.Email;
                    recipientName = $"{violation.PolicyAssignment.User.FirstName} {violation.PolicyAssignment.User.LastName}";
                    policyTitle = violation.PolicyAssignment.Policy?.PolicyTitle ?? "N/A";
                }
                else if (violation.ViolationType == "Supplier" && violation.SupplierPolicy?.Supplier != null)
                {
                    recipientEmail = violation.SupplierPolicy.Supplier.Email;
                    var s = violation.SupplierPolicy.Supplier;
                    recipientName = !string.IsNullOrWhiteSpace(s.ContactPersonFirstName)
                        ? $"{s.ContactPersonFirstName} {s.ContactPersonLastName}".Trim() : s.SupplierName;
                    policyTitle = violation.SupplierPolicy.Policy?.PolicyTitle ?? "N/A";
                }

                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    var oldLabel = oldStatus == "UnderReview" ? "Under Review" : oldStatus;
                    var extra = "";
                    if (!string.IsNullOrWhiteSpace(request.Notes)) extra += $"\nNote: {request.Notes}";
                    if (newStatus == "Resolved" && !string.IsNullOrWhiteSpace(request.Resolution)) extra += $"\nResolution: {request.Resolution}";
                    var body = $"Dear {recipientName},\n\nYour compliance violation (#{violationId}) status has been updated.\n\nPolicy: {policyTitle}\nPrevious Status: {oldLabel}\nNew Status: {statusLabel}\nUpdated On: {nowStr} UTC{extra}\n\nPlease contact your compliance officer if you have any questions.\n\n— TechNova IT Solutions Compliance Team";
                    await _emailService.SendEmailAsync(recipientEmail,
                        $"[TechNova] Compliance Violation #{violationId} – Status Updated to {statusLabel}", body);
                }
            }
            catch { /* email failure is non-blocking */ }

            return Ok(new { success = true, message = "Violation updated" });
        }

        // ── Violation subject / policy lookup dropdowns ────────────────────

        [HttpGet]
        public async Task<IActionResult> GetViolationSubjects(string type)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            if (type == "Employee")
            {
                var subjects = await _context.PolicyAssignments
                    .Include(pa => pa.User)
                    .Where(pa => pa.User != null)
                    .Where(pa => !scoped || pa.User.BranchId == callerBranchId)
                    .GroupBy(pa => pa.UserId)
                    .Select(g => new
                    {
                        SubjectId = g.Key,
                        Name = g.First().User!.FirstName + " " + g.First().User!.LastName,
                        Email = g.First().User!.Email
                    })
                    .OrderBy(s => s.Name)
                    .ToListAsync();
                return Ok(new { success = true, subjects });
            }
            else
            {
                var subjects = await _context.SupplierPolicies
                    .Include(sp => sp.Supplier)
                    .Where(sp => sp.Supplier != null)
                    .Where(sp => !scoped || sp.Supplier.BranchId == callerBranchId || sp.Supplier.BranchId == null)
                    .GroupBy(sp => sp.SupplierId)
                    .Select(g => new
                    {
                        SubjectId = g.Key,
                        Name = g.First().Supplier!.SupplierName
                    })
                    .OrderBy(s => s.Name)
                    .ToListAsync();
                return Ok(new { success = true, subjects });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjectPolicies(string type, int subjectId)
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            if (type == "Employee")
            {
                var policies = await _context.PolicyAssignments
                    .Include(pa => pa.Policy)
                    .Where(pa => pa.UserId == subjectId && pa.Policy != null)
                    .Select(pa => new { Id = pa.AssignmentId, Title = pa.Policy!.PolicyTitle })
                    .OrderBy(p => p.Title)
                    .ToListAsync();
                return Ok(new { success = true, policies });
            }
            else
            {
                var policies = await _context.SupplierPolicies
                    .Include(sp => sp.Policy)
                    .Where(sp => sp.SupplierId == subjectId && sp.Policy != null)
                    .Select(sp => new { Id = sp.SupplierPolicyId, Title = sp.Policy!.PolicyTitle })
                    .OrderBy(p => p.Title)
                    .ToListAsync();
                return Ok(new { success = true, policies });
            }
        }

        // ── Compliant auto-detect ───────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetCompliantEmployees()
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            var rows = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged")
                .Where(pa => !scoped || pa.User.BranchId == callerBranchId)
                .OrderBy(pa => pa.User.FirstName)
                .Select(pa => new
                {
                    pa.AssignmentId,
                    UserId = pa.User != null ? pa.User.UserId : 0,
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    Email = pa.User != null ? pa.User.Email : "",
                    UserStatus = pa.User != null ? pa.User.Status : "Active",
                    PolicyTitle = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    AssignedDate = pa.AssignedDate.HasValue ? pa.AssignedDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    AcknowledgedDate = pa.ComplianceStatus!.AcknowledgedDate.HasValue
                        ? pa.ComplianceStatus.AcknowledgedDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    Status = "Acknowledged"
                })
                .ToListAsync();

            return Ok(new { success = true, employees = rows });
        }

        [HttpGet]
        public async Task<IActionResult> GetCompliantSuppliers()
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            var rows = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .Where(sp => sp.ComplianceStatus == "Compliant")
                .Where(sp => !scoped || sp.Supplier.BranchId == callerBranchId || sp.Supplier.BranchId == null)
                .OrderBy(sp => sp.Supplier.SupplierName)
                .Select(sp => new
                {
                    sp.SupplierPolicyId,
                    SupplierName = sp.Supplier.SupplierName,
                    SupplierStatus = sp.Supplier.Status,
                    PolicyTitle = sp.Policy != null ? sp.Policy.PolicyTitle : "Unknown",
                    AssignedDate = sp.AssignedDate.HasValue ? sp.AssignedDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    ComplianceStatus = "Compliant",
                    SupplierId = sp.SupplierId
                })
                .ToListAsync();

            return Ok(new { success = true, suppliers = rows });
        }

        // ── Non-compliant auto-detect ───────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetNonCompliantEmployees()
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            var rows = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.ComplianceStatus == null || pa.ComplianceStatus.Status != "Acknowledged")
                .Where(pa => !scoped || pa.User.BranchId == callerBranchId)
                .OrderBy(pa => pa.User.FirstName)
                .Select(pa => new
                {
                    pa.AssignmentId,
                    UserId = pa.User != null ? pa.User.UserId : 0,
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    Email = pa.User != null ? pa.User.Email : "",
                    UserStatus = pa.User != null ? pa.User.Status : "Active",
                    PolicyTitle = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    AssignedDate = pa.AssignedDate.HasValue ? pa.AssignedDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    Status = pa.ComplianceStatus != null ? pa.ComplianceStatus.Status : "Pending",
                    HasViolation = _context.ComplianceViolations.Any(v => v.PolicyAssignmentId == pa.AssignmentId && v.Status != "Resolved")
                })
                .ToListAsync();

            return Ok(new { success = true, employees = rows });
        }

        [HttpGet]
        public async Task<IActionResult> GetNonCompliantSuppliers()
        {
            if (!HasPolicyAssignmentAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var callerBranchId = HasGlobalScope() ? (int?)null : GetCallerBranchId();
            bool scoped = callerBranchId.HasValue;

            var rows = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .Where(sp => sp.ComplianceStatus != "Compliant")
                .Where(sp => !scoped || sp.Supplier.BranchId == callerBranchId || sp.Supplier.BranchId == null)
                .OrderBy(sp => sp.Supplier.SupplierName)
                .Select(sp => new
                {
                    sp.SupplierPolicyId,
                    SupplierName = sp.Supplier.SupplierName,
                    SupplierStatus = sp.Supplier.Status,
                    PolicyTitle = sp.Policy != null ? sp.Policy.PolicyTitle : "Unknown",
                    AssignedDate = sp.AssignedDate.HasValue ? sp.AssignedDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    ComplianceStatus = sp.ComplianceStatus ?? "Pending",
                    SupplierId = sp.SupplierId,
                    HasViolation = _context.ComplianceViolations.Any(v => v.SupplierPolicyId == sp.SupplierPolicyId && v.Status != "Resolved")
                })
                .ToListAsync();

            return Ok(new { success = true, suppliers = rows });
        }
    }

    // ── Request DTOs ────────────────────────────────────────────

    public class EmployeeSuspendRequest
    {
        public string? Reason { get; set; }
    }

    public class SupplierSuspendRequest
    {
        public string? Reason { get; set; }
    }

    public class CreateViolationRequest
    {
        public string? ViolationType { get; set; }
        public int? PolicyAssignmentId { get; set; }
        public int? SupplierPolicyId { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateViolationStatusRequest
    {
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public string? Resolution { get; set; }
    }
}
