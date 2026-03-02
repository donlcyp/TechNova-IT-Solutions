using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Employee, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var data = await _employeeService.GetEmployeeDashboardDataAsync(userId);
            return View(data);
        }

        public async Task<IActionResult> ComplianceStatus()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Employee, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var data = await _employeeService.GetEmployeeComplianceStatusAsync(userId);
            return View(data);
        }

        public async Task<IActionResult> AssignedPolicies()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Employee, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var policies = await _employeeService.GetAssignedPoliciesAsync(userId);
            return View(policies);
        }

        public IActionResult Settings()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Employee, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcknowledgePolicy([FromBody] AcknowledgeRequest request)
        {
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.Employee, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (unauthorized != null) return unauthorized;

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { success = false, message = "Not logged in" });

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { success = false, message = "Invalid session" });

            var result = await _employeeService.AcknowledgePolicyAsync(userId, request.PolicyId);
            return result
                ? Ok(new { success = true, message = "Policy acknowledged successfully" })
                : BadRequest(new { success = false, message = "Failed to acknowledge policy" });
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeNotifications()
        {
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.Employee, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (unauthorized != null) return unauthorized;

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { success = false });

            // Pending policy acknowledgements
            var pendingPolicies = await _employeeService.GetPendingPoliciesCountAsync(userId);

            // Active violations (not resolved) linked to this employee's assignments
            var activeViolations = await _employeeService.GetActiveViolationsCountAsync(userId);

            return Ok(new EmpNotificationResult
            {
                Success = true,
                PendingPolicies = pendingPolicies,
                ActiveViolations = activeViolations
            });
        }
    }

    public class AcknowledgeRequest
    {
        public int PolicyId { get; set; }
    }

    public class EmpNotificationResult
    {
        public bool Success { get; set; }
        public int PendingPolicies { get; set; }
        public int ActiveViolations { get; set; }
        public int Total => PendingPolicies + ActiveViolations;
    }
}
