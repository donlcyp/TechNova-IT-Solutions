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

        private bool HasPolicyAuthority()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            return role == RoleNames.ChiefComplianceManager || role == RoleNames.ComplianceManager || role == RoleNames.Admin || role == RoleNames.SuperAdmin;
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            policyData.UploadedDate ??= DateTime.Now;

            // Branch Admins/CMs automatically stamp their branch; SuperAdmin/CCM keeps null (company-wide)
            if (!HasGlobalScope())
            {
                policyData.BranchId = GetCallerBranchId();
            }

            var result = await _adminService.CreatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created policy: {policyData.PolicyTitle}", "Policy");
                return Ok(new { success = true, message = "Policy created successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to create policy" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePolicy([FromBody] PolicyData policyData)
        {
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            var result = await _adminService.UpdatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated policy: {policyData.PolicyTitle}", "Policy");
                var emailSummary = new EmailNotificationSummary();
                if (policyData.PolicyId > 0)
                    emailSummary = await NotifyEmployeesPolicyUpdatedAsync(policyData.PolicyId, policyData.PolicyTitle);

                return Ok(new
                {
                    success = true,
                    message = $"Policy updated. Email sent: {emailSummary.SentCount}, failed: {emailSummary.FailedCount}.",
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
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
                UploadedDate = DateTime.Now
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return NotFound(new { success = false, message = "Supplier not found" });

            supplier.Status = "Suspended";
            supplier.TerminationReason = request?.Reason ?? "Suspended for non-compliance by Compliance Manager";
            supplier.TerminatedAt = DateTime.UtcNow;
            supplier.TerminatedByUserId = GetCurrentUserId();
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(), $"Suspended supplier: {supplier.SupplierName} — {supplier.TerminationReason}", "Compliance");
            return Ok(new { success = true, message = $"Supplier '{supplier.SupplierName}' has been suspended." });
        }

        [HttpPost]
        public async Task<IActionResult> UnsuspendSupplier(int supplierId)
        {
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return NotFound(new { success = false, message = "Supplier not found" });

            supplier.Status = "Active";
            supplier.TerminationReason = null;
            supplier.TerminatedAt = null;
            supplier.TerminatedByUserId = null;
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(), $"Re-activated supplier: {supplier.SupplierName}", "Compliance");
            return Ok(new { success = true, message = $"Supplier '{supplier.SupplierName}' has been re-activated." });
        }

        // ── Violation management endpoints ──────────────────────

        [HttpGet]
        public async Task<IActionResult> GetViolations(string? type, string? status)
        {
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

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

        [HttpPost]
        public async Task<IActionResult> CreateViolation([FromBody] CreateViolationRequest request)
        {
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });
            if (request == null) return BadRequest(new { success = false, message = "Invalid request" });

            var violation = new Models.ComplianceViolation
            {
                ViolationType = request.ViolationType ?? "Employee",
                PolicyAssignmentId = request.PolicyAssignmentId,
                SupplierPolicyId = request.SupplierPolicyId,
                Description = request.Description,
                Notes = request.Notes,
                Status = "Open",
                RaisedDate = DateTime.UtcNow,
                RaisedByUserId = GetCurrentUserId()
            };

            _context.ComplianceViolations.Add(violation);
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Raised compliance violation #{violation.ViolationId}: {request.Description}", "Compliance");

            return Ok(new { success = true, message = "Violation created successfully", violationId = violation.ViolationId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateViolationStatus(int violationId, [FromBody] UpdateViolationStatusRequest request)
        {
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

            var violation = await _context.ComplianceViolations.FindAsync(violationId);
            if (violation == null) return NotFound(new { success = false, message = "Violation not found" });

            violation.Status = request.Status ?? violation.Status;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                violation.Notes = (violation.Notes ?? "") + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.Notes}";
            if (!string.IsNullOrWhiteSpace(request.Resolution))
                violation.Resolution = request.Resolution;
            if (request.Status == "Resolved")
                violation.ResolvedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Updated violation #{violationId} → {violation.Status}", "Compliance");

            return Ok(new { success = true, message = "Violation updated" });
        }

        // ── Non-compliant auto-detect ───────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetNonCompliantEmployees()
        {
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

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
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    Email = pa.User != null ? pa.User.Email : "",
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
            if (!HasPolicyAuthority()) return Unauthorized(new { success = false, message = "Access denied" });

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
