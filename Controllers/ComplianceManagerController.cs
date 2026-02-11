using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class ComplianceManagerController : Controller
    {
        private readonly IComplianceManagerService _complianceService;

        public ComplianceManagerController(IComplianceManagerService complianceService)
        {
            _complianceService = complianceService;
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
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var data = await _complianceService.GetComplianceDashboardDataAsync();
            return View(data);
        }

        public async Task<IActionResult> EmployeeCompliance()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var data = await _complianceService.GetEmployeeComplianceReportAsync();
            return View(data);
        }

        public async Task<IActionResult> AuditTrail()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var data = await _complianceService.GetAuditTrailDataAsync();
            return View(data);
        }

        public async Task<IActionResult> ComplianceReports()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var data = await _complianceService.GetComplianceReportsDataAsync();
            return View(data);
        }

        public IActionResult PolicyReview()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        public IActionResult SupplierCompliance()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
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
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        public async Task<IActionResult> ExternalPolicyReferences(string category = "all")
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Fetch external policy references by category
            var externalPolicies = await _complianceService.GetExternalPolicyReferencesAsync(category);
            
            ViewBag.Category = category;
            ViewBag.UserRole = userRole;
            return View(externalPolicies);
        }

        [HttpPost]
        public async Task<IActionResult> GetExternalPolicyJson(string policyId)
        {
            // Check role - only ComplianceManager and Admin can access
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                return Unauthorized(new { success = false, message = "Access denied" });
            }

            // API endpoint for AJAX calls to get specific policy details
            var externalPolicies = await _complianceService.GetExternalPolicyReferencesAsync("");
            var policy = externalPolicies.FirstOrDefault(p => p.PolicyTitle.Contains(policyId));
            
            return Json(policy);
        }
    }
}
