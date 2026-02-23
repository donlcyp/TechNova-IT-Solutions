using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    [ApiController]
    [Route("api/policies")]
    public class PoliciesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeService _employeeService;

        public PoliciesController(ApplicationDbContext context, IEmployeeService employeeService)
        {
            _context = context;
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicies()
        {
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.Employee,
                RoleNames.Admin,
                RoleNames.SuperAdmin,
                RoleNames.ComplianceManager);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var policies = await _context.Policies
                .AsNoTracking()
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new PolicyListItemResponse
                {
                    PolicyId = p.PolicyId,
                    PolicyTitle = p.PolicyTitle,
                    Category = p.Category,
                    Description = p.Description,
                    FilePath = p.FilePath,
                    DateUploaded = p.DateUploaded,
                    IsArchived = p.IsArchived,
                    AssignedCount = p.PolicyAssignments.Count,
                    AcknowledgedCount = p.PolicyAssignments.Count(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged")
                })
                .ToListAsync();

            return Ok(policies);
        }

        [HttpGet("{id:int}/versions")]
        public async Task<IActionResult> GetPolicyVersions(int id)
        {
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.Employee,
                RoleNames.Admin,
                RoleNames.SuperAdmin,
                RoleNames.ComplianceManager);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var policy = await _context.Policies
                .AsNoTracking()
                .Include(p => p.UploadedByUser)
                .FirstOrDefaultAsync(p => p.PolicyId == id);

            if (policy == null)
            {
                return NotFound(new { success = false, message = "Policy not found" });
            }

            var logs = await _context.AuditLogs
                .AsNoTracking()
                .Include(al => al.User)
                .Where(al =>
                    al.Module == "Policy" &&
                    al.Action != null &&
                    (EF.Functions.Like(al.Action, "%" + policy.PolicyTitle + "%") ||
                     EF.Functions.Like(al.Action, "%ID: " + id + "%") ||
                     EF.Functions.Like(al.Action, "%policy: " + id + "%")))
                .OrderBy(al => al.LogDate)
                .ToListAsync();

            var versions = logs
                .Select((log, index) => new PolicyVersionResponse
                {
                    VersionNumber = index + 1,
                    ChangeType = MapChangeType(log.Action),
                    ChangedAt = log.LogDate,
                    ChangedBy = log.User != null
                        ? (log.User.FirstName + " " + log.User.LastName).Trim()
                        : "System",
                    Notes = log.Action
                })
                .ToList();

            if (!versions.Any())
            {
                versions.Add(new PolicyVersionResponse
                {
                    VersionNumber = 1,
                    ChangeType = "Created",
                    ChangedAt = policy.DateUploaded ?? DateTime.Now,
                    ChangedBy = policy.UploadedByUser != null
                        ? (policy.UploadedByUser.FirstName + " " + policy.UploadedByUser.LastName).Trim()
                        : "System",
                    Notes = "Initial policy record"
                });
            }

            return Ok(new
            {
                policyId = policy.PolicyId,
                policyTitle = policy.PolicyTitle,
                versions
            });
        }

        [HttpGet("{id:int}/acknowledgments")]
        public async Task<IActionResult> GetPolicyAcknowledgments(int id)
        {
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.Employee,
                RoleNames.Admin,
                RoleNames.SuperAdmin,
                RoleNames.ComplianceManager);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var policy = await _context.Policies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PolicyId == id);

            if (policy == null)
            {
                return NotFound(new { success = false, message = "Policy not found" });
            }

            var acknowledgments = await _context.PolicyAssignments
                .AsNoTracking()
                .Include(pa => pa.User)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.PolicyId == id)
                .OrderBy(pa => pa.AssignedDate)
                .Select(pa => new PolicyAcknowledgmentResponse
                {
                    AssignmentId = pa.AssignmentId,
                    UserId = pa.UserId,
                    UserName = pa.User != null ? (pa.User.FirstName + " " + pa.User.LastName).Trim() : "Unknown",
                    UserEmail = pa.User != null ? pa.User.Email : null,
                    Status = pa.ComplianceStatus != null ? pa.ComplianceStatus.Status : "Pending",
                    AssignedDate = pa.AssignedDate,
                    AcknowledgedDate = pa.ComplianceStatus != null ? pa.ComplianceStatus.AcknowledgedDate : null
                })
                .ToListAsync();

            return Ok(new
            {
                policyId = policy.PolicyId,
                policyTitle = policy.PolicyTitle,
                totalAssignments = acknowledgments.Count,
                acknowledgedCount = acknowledgments.Count(a => a.Status == "Acknowledged"),
                pendingCount = acknowledgments.Count(a => a.Status != "Acknowledged"),
                acknowledgments
            });
        }

        [HttpPost("{id:int}/acknowledgments")]
        public async Task<IActionResult> AcknowledgePolicy(int id, [FromBody] PostPolicyAcknowledgmentRequest? request)
        {
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.Employee,
                RoleNames.Admin,
                RoleNames.SuperAdmin,
                RoleNames.ComplianceManager);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrWhiteSpace(userIdString) || !int.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            var targetUserId = currentUserId;
            var canAcknowledgeForOtherUsers = RoleAccess.HasAnyRole(HttpContext, RoleNames.Admin, RoleNames.SuperAdmin, RoleNames.ComplianceManager);
            if (request?.UserId is > 0 && canAcknowledgeForOtherUsers)
            {
                targetUserId = request.UserId.Value;
            }

            var acknowledged = await _employeeService.AcknowledgePolicyAsync(targetUserId, id);
            if (!acknowledged)
            {
                return BadRequest(new { success = false, message = "Failed to acknowledge policy" });
            }

            return Ok(new
            {
                success = true,
                policyId = id,
                userId = targetUserId,
                acknowledgedAt = DateTime.Now
            });
        }

        private static string MapChangeType(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return "Updated";
            }

            if (action.Contains("Created policy", StringComparison.OrdinalIgnoreCase))
            {
                return "Created";
            }

            if (action.Contains("Updated policy", StringComparison.OrdinalIgnoreCase))
            {
                return "Updated";
            }

            if (action.Contains("Archived policy", StringComparison.OrdinalIgnoreCase))
            {
                return "Archived";
            }

            if (action.Contains("Restored policy", StringComparison.OrdinalIgnoreCase))
            {
                return "Restored";
            }

            if (action.Contains("Deleted policy", StringComparison.OrdinalIgnoreCase))
            {
                return "Deleted";
            }

            return "Updated";
        }

        public class PolicyListItemResponse
        {
            public int PolicyId { get; set; }
            public string PolicyTitle { get; set; } = string.Empty;
            public string? Category { get; set; }
            public string? Description { get; set; }
            public string? FilePath { get; set; }
            public DateTime? DateUploaded { get; set; }
            public bool IsArchived { get; set; }
            public int AssignedCount { get; set; }
            public int AcknowledgedCount { get; set; }
        }

        public class PolicyVersionResponse
        {
            public int VersionNumber { get; set; }
            public string ChangeType { get; set; } = string.Empty;
            public DateTime ChangedAt { get; set; }
            public string ChangedBy { get; set; } = string.Empty;
            public string? Notes { get; set; }
        }

        public class PolicyAcknowledgmentResponse
        {
            public int AssignmentId { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string? UserEmail { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime? AssignedDate { get; set; }
            public DateTime? AcknowledgedDate { get; set; }
        }

        public class PostPolicyAcknowledgmentRequest
        {
            public int? UserId { get; set; }
        }
    }
}
