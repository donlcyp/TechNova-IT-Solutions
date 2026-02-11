using Microsoft.AspNetCore.Mvc;
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
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Employee" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var data = await _employeeService.GetEmployeeDashboardDataAsync(userId);
            return View(data);
        }

        public async Task<IActionResult> ComplianceStatus()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Employee" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var data = await _employeeService.GetEmployeeComplianceStatusAsync(userId);
            return View(data);
        }

        public async Task<IActionResult> AssignedPolicies()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Employee" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var policies = await _employeeService.GetAssignedPoliciesAsync(userId);
            return View(policies);
        }

        public IActionResult Settings()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Employee" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcknowledgePolicy([FromBody] AcknowledgeRequest request)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { success = false, message = "Not logged in" });

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { success = false, message = "Invalid session" });

            var result = await _employeeService.AcknowledgePolicyAsync(userId, request.PolicyId);
            return result
                ? Ok(new { success = true, message = "Policy acknowledged successfully" })
                : BadRequest(new { success = false, message = "Failed to acknowledge policy" });
        }
    }

    public class AcknowledgeRequest
    {
        public int PolicyId { get; set; }
    }
}
