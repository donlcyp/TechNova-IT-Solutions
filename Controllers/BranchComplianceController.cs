using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    /// <summary>
    /// Handles branch-level compliance assignment:
    ///   - Assign policies to branch employees
    ///   - Assign contract policies to branch suppliers
    /// </summary>
    public class BranchComplianceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminService _adminService;
        private readonly IEmailService _emailService;

        public BranchComplianceController(
            ApplicationDbContext context,
            IAdminService adminService,
            IEmailService emailService)
        {
            _context = context;
            _adminService = adminService;
            _emailService = emailService;
        }

        // ── Auth helpers ─────────────────────────────────────────────

        /// <summary>
        /// Branch compliance assignment is a Compliance Manager responsibility.
        /// Admin should manage operations (procurement, suppliers, users), not compliance assignments.
        /// </summary>
        private bool HasAccess()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            return role == RoleNames.SuperAdmin
                || role == RoleNames.ComplianceManager || role == RoleNames.ChiefComplianceManager;
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

        private bool IsGlobalRole()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            return role == RoleNames.SuperAdmin || role == RoleNames.ChiefComplianceManager;
        }

        // ── Data endpoints ────────────────────────────────────────────

        /// <summary>
        /// Returns employees in the caller's branch with their policy-compliance summary.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBranchEmployees(string? search)
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });

            var branchId = IsGlobalRole() ? (int?)null : GetCallerBranchId();
            bool scoped = branchId.HasValue;

            var query = _context.Users
                .Where(u => u.Role == RoleNames.Employee && u.Status == "Active")
                .Where(u => !scoped || u.BranchId == branchId)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)));
            }

            var users = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new
                {
                    u.UserId,
                    FullName = u.FirstName + " " + u.LastName,
                    u.Email,
                    u.Role,
                    u.BranchId,
                    u.Status
                })
                .ToListAsync();

            var userIds = users.Select(u => u.UserId).ToList();

            // Pull assignment info
            var assignments = await _context.PolicyAssignments
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => userIds.Contains(pa.UserId))
                .AsNoTracking()
                .ToListAsync();

            var result = users.Select(u =>
            {
                var ua = assignments.Where(a => a.UserId == u.UserId).ToList();
                var compliant = ua.Count(a => a.ComplianceStatus?.Status == "Acknowledged");
                return new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.Status,
                    TotalPolicies = ua.Count,
                    CompliantCount = compliant,
                    NonCompliantCount = ua.Count - compliant,
                    OverallStatus = ua.Count == 0 ? "No Policies"
                        : compliant == ua.Count ? "Compliant"
                        : "Not Compliant",
                    AssignedPolicies = ua.Select(a => new
                    {
                        a.AssignmentId,
                        PolicyId = a.PolicyId,
                        PolicyTitle = a.Policy?.PolicyTitle ?? "Unknown",
                        AssignedDate = a.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        Status = a.ComplianceStatus?.Status == "Acknowledged" ? "Compliant" : "Not Compliant",
                        AcknowledgedDate = a.ComplianceStatus?.AcknowledgedDate?.ToString("yyyy-MM-dd") ?? "Pending"
                    }).ToList()
                };
            });

            return Ok(new { success = true, employees = result });
        }

        /// <summary>
        /// Returns suppliers linked to the caller's branch with their contract-policy summary.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBranchSuppliers(string? search)
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });

            var branchId = IsGlobalRole() ? (int?)null : GetCallerBranchId();
            bool scoped = branchId.HasValue;

            var query = _context.Suppliers
                .Where(s => s.Status != "Terminated")
                .Where(s => !scoped || s.BranchId == branchId || s.BranchId == null)
                .Include(s => s.SupplierPolicies)
                .ThenInclude(sp => sp.Policy)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(sup =>
                    sup.SupplierName.ToLower().Contains(s) ||
                    (sup.Email != null && sup.Email.ToLower().Contains(s)));
            }

            var suppliers = await query
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            var result = suppliers.Select(s =>
            {
                var policies = s.SupplierPolicies.ToList();
                var compliant = policies.Count(sp => sp.ComplianceStatus != null &&
                    sp.ComplianceStatus.Equals("Compliant", StringComparison.OrdinalIgnoreCase));
                return new
                {
                    s.SupplierId,
                    s.SupplierName,
                    ContactPerson = $"{s.ContactPersonFirstName} {s.ContactPersonLastName}".Trim(),
                    s.Email,
                    s.Status,
                    s.BranchId,
                    TotalContracts = policies.Count,
                    CompliantCount = compliant,
                    NonCompliantCount = policies.Count - compliant,
                    OverallStatus = policies.Count == 0 ? "No Contracts"
                        : compliant == policies.Count ? "Compliant"
                        : "Not Compliant",
                    AssignedPolicies = policies.Select(sp => new
                    {
                        sp.SupplierPolicyId,
                        sp.PolicyId,
                        PolicyTitle = sp.Policy?.PolicyTitle ?? "Unknown",
                        AssignedDate = sp.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        Status = sp.ComplianceStatus == "Compliant" ? "Compliant" : "Not Compliant"
                    }).ToList()
                };
            });

            return Ok(new { success = true, suppliers = result });
        }

        /// <summary>
        /// Returns active (non-archived) policies visible to this branch.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAvailablePolicies()
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });

            var branchId = IsGlobalRole() ? (int?)null : GetCallerBranchId();

            var policies = await _context.Policies
                .Where(p => !p.IsArchived && p.ReviewStatus == "Approved")
                .Where(p => p.BranchId == null || p.BranchId == branchId)
                .OrderBy(p => p.PolicyTitle)
                .Select(p => new
                {
                    p.PolicyId,
                    p.PolicyTitle,
                    p.Category,
                    Scope = p.BranchId == null ? "Company-Wide" : "Branch"
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(new { success = true, policies });
        }

        /// <summary>
        /// Returns a summary count: employees assigned, compliant, suppliers assigned, compliant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSummary()
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });

            var branchId = IsGlobalRole() ? (int?)null : GetCallerBranchId();
            bool scoped = branchId.HasValue;

            var totalEmployees = await _context.Users
                .Where(u => u.Role == RoleNames.Employee && u.Status == "Active"
                    && (!scoped || u.BranchId == branchId))
                .CountAsync();

            var assignedEmployees = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Where(pa => pa.User.Status == "Active"
                    && (!scoped || pa.User.BranchId == branchId))
                .Select(pa => pa.UserId)
                .Distinct()
                .CountAsync();

            var compliantEmployees = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.User.Status == "Active"
                    && (!scoped || pa.User.BranchId == branchId)
                    && pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged")
                .Select(pa => pa.UserId)
                .Distinct()
                .CountAsync();

            var totalSuppliers = await _context.Suppliers
                .Where(s => s.Status != "Terminated"
                    && (!scoped || s.BranchId == branchId || s.BranchId == null))
                .CountAsync();

            var compliantSuppliers = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Where(sp => sp.Supplier.Status != "Terminated"
                    && (!scoped || sp.Supplier.BranchId == branchId || sp.Supplier.BranchId == null)
                    && sp.ComplianceStatus == "Compliant")
                .Select(sp => sp.SupplierId)
                .Distinct()
                .CountAsync();

            var activeContracts = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Where(sp => sp.Supplier.Status != "Terminated"
                    && (!scoped || sp.Supplier.BranchId == branchId || sp.Supplier.BranchId == null))
                .CountAsync();

            return Ok(new
            {
                success = true,
                totalEmployees,
                assignedEmployees,
                compliantEmployees,
                totalSuppliers,
                compliantSuppliers,
                activeContracts
            });
        }

        // ── Assignment endpoints ──────────────────────────────────────

        /// <summary>
        /// Assign one or more policies to one or more employees.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssignPoliciesToEmployees([FromBody] BranchPolicyAssignRequest req)
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });
            if (req == null || !req.PolicyIds.Any() || !req.TargetIds.Any())
                return BadRequest(new { success = false, message = "Policy IDs and employee IDs are required." });

            // Branch guard: employees must belong to caller's branch
            if (!IsGlobalRole())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue)
                {
                    var outOfBranch = await _context.Users
                        .AnyAsync(u => req.TargetIds.Contains(u.UserId) && u.BranchId != callerBranchId);
                    if (outOfBranch)
                        return BadRequest(new { success = false, message = "You can only assign policies to employees in your branch." });
                }
            }

            bool allOk = true;
            foreach (var pid in req.PolicyIds.Distinct())
                allOk &= await _adminService.AssignPolicyToEmployeesAsync(pid, req.TargetIds);

            if (allOk)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(),
                    $"Branch Compliance: assigned {req.PolicyIds.Count} policy(ies) to {req.TargetIds.Count} employee(s).",
                    "Compliance");

                // Notify employees via email
                int sent = 0, failed = 0;
                var employees = await _context.Users
                    .Where(u => req.TargetIds.Contains(u.UserId) && !string.IsNullOrEmpty(u.Email))
                    .ToListAsync();

                var policyTitles = await _context.Policies
                    .Where(p => req.PolicyIds.Contains(p.PolicyId))
                    .Select(p => p.PolicyTitle)
                    .ToListAsync();
                var policyText = string.Join(", ", policyTitles);

                foreach (var emp in employees)
                {
                    var body = $@"
                        <h2>New Policy Assigned</h2>
                        <p>Hello {emp.FirstName},</p>
                        <p>The following compliance policy/policies have been assigned to you:</p>
                        <p><strong>{policyText}</strong></p>
                        <p>Please log in and acknowledge them at your earliest convenience.</p>";
                    var r = await _emailService.SendEmailAsync(emp.Email!, "New Policy Assignment", body);
                    if (r.Success) sent++; else failed++;
                }

                return Ok(new
                {
                    success = true,
                    message = $"Successfully assigned {req.PolicyIds.Count} policy(ies) to {req.TargetIds.Count} employee(s). Email sent: {sent}, failed: {failed}.",
                    emailSent = sent,
                    emailFailed = failed
                });
            }

            return BadRequest(new { success = false, message = "Assignment failed for one or more policies." });
        }

        /// <summary>
        /// Assign one or more contract/policies to one or more suppliers.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssignPoliciesToSuppliers([FromBody] BranchPolicyAssignRequest req)
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });
            if (req == null || !req.PolicyIds.Any() || !req.TargetIds.Any())
                return BadRequest(new { success = false, message = "Policy IDs and supplier IDs are required." });

            // Branch guard: suppliers must belong to caller's branch (or be global)
            if (!IsGlobalRole())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue)
                {
                    var invalid = await _context.Suppliers
                        .AnyAsync(s => req.TargetIds.Contains(s.SupplierId)
                            && s.BranchId != callerBranchId && s.BranchId != null);
                    if (invalid)
                        return BadRequest(new { success = false, message = "You can only assign contracts to your branch's suppliers or global suppliers." });
                }
            }

            bool allOk = true;
            foreach (var pid in req.PolicyIds.Distinct())
                allOk &= await _adminService.AssignPolicyToSuppliersAsync(pid, req.TargetIds);

            if (allOk)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(),
                    $"Branch Compliance: assigned {req.PolicyIds.Count} contract policy(ies) to {req.TargetIds.Count} supplier(s).",
                    "Compliance");

                return Ok(new
                {
                    success = true,
                    message = $"Successfully assigned {req.PolicyIds.Count} contract policy(ies) to {req.TargetIds.Count} supplier(s)."
                });
            }

            return BadRequest(new { success = false, message = "Assignment failed for one or more contracts." });
        }

        /// <summary>
        /// Remove a policy assignment from an employee.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveEmployeeAssignment(int assignmentId)
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });

            var assignment = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .FirstOrDefaultAsync(pa => pa.AssignmentId == assignmentId);

            if (assignment == null)
                return NotFound(new { success = false, message = "Assignment not found." });

            // Branch guard
            if (!IsGlobalRole())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue && assignment.User?.BranchId != callerBranchId)
                    return BadRequest(new { success = false, message = "You can only remove assignments within your branch." });
            }

            // Remove compliance status if exists
            var cs = await _context.ComplianceStatuses.FirstOrDefaultAsync(c => c.AssignmentId == assignmentId);
            if (cs != null) _context.ComplianceStatuses.Remove(cs);

            _context.PolicyAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Branch Compliance: removed policy assignment #{assignmentId} from employee.", "Compliance");

            return Ok(new { success = true, message = "Policy assignment removed." });
        }

        /// <summary>
        /// Remove a supplier policy (contract) assignment.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveSupplierAssignment(int supplierPolicyId)
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });

            var sp = await _context.SupplierPolicies
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.SupplierPolicyId == supplierPolicyId);

            if (sp == null)
                return NotFound(new { success = false, message = "Supplier policy assignment not found." });

            // Branch guard
            if (!IsGlobalRole())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue && sp.Supplier?.BranchId != callerBranchId && sp.Supplier?.BranchId != null)
                    return BadRequest(new { success = false, message = "You can only remove assignments within your branch." });
            }

            _context.SupplierPolicies.Remove(sp);
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Branch Compliance: removed contract assignment #{supplierPolicyId} from supplier.", "Compliance");

            return Ok(new { success = true, message = "Contract assignment removed." });
        }

        /// <summary>
        /// Update supplier policy compliance status (Pending / Compliant / Non-Compliant).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateSupplierComplianceStatus(int supplierPolicyId, [FromBody] SupplierStatusUpdateRequest req)
        {
            if (!HasAccess()) return Unauthorized(new { success = false, message = "Access denied" });

            var sp = await _context.SupplierPolicies
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.SupplierPolicyId == supplierPolicyId);

            if (sp == null)
                return NotFound(new { success = false, message = "Supplier policy assignment not found." });

            if (!IsGlobalRole())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue && sp.Supplier?.BranchId != callerBranchId && sp.Supplier?.BranchId != null)
                    return BadRequest(new { success = false, message = "Access denied to this supplier." });
            }

            var allowed = new[] { "Pending", "Compliant", "Non-Compliant" };
            if (req?.Status == null || !allowed.Contains(req.Status))
                return BadRequest(new { success = false, message = "Invalid status. Must be Pending, Compliant, or Non-Compliant." });

            sp.ComplianceStatus = req.Status;
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(),
                $"Branch Compliance: updated supplier contract #{supplierPolicyId} status to {req.Status}.", "Compliance");

            return Ok(new { success = true, message = $"Contract status updated to {req.Status}." });
        }
    }

    // ── Request DTOs ─────────────────────────────────────────────────

    public class BranchPolicyAssignRequest
    {
        public List<int> PolicyIds { get; set; } = new();
        public List<int> TargetIds { get; set; } = new();
    }

    public class SupplierStatusUpdateRequest
    {
        public string? Status { get; set; }
    }
}
