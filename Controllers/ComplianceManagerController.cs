using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Infrastructure;
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
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var data = await _complianceService.GetComplianceDashboardDataAsync();
            return View(data);
        }

        public async Task<IActionResult> EmployeeCompliance()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var data = await _complianceService.GetEmployeeComplianceReportAsync();
            return View(data);
        }

        public async Task<IActionResult> AuditTrail()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var data = await _complianceService.GetAuditTrailDataAsync();
            return View(data);
        }

        public async Task<IActionResult> ComplianceReports()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var data = await _complianceService.GetComplianceReportsDataAsync();
            return View(data);
        }

        public IActionResult PolicyReview()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult SupplierCompliance()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult Settings()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public async Task<IActionResult> ExternalPolicyReferences(string category = "all")
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole) ?? string.Empty;

            // Fetch external policy references by category
            var externalPolicies = await _complianceService.GetExternalPolicyReferencesAsync(category);
            
            ViewBag.Category = category;
            ViewBag.UserRole = userRole;
            return View(externalPolicies);
        }

        [HttpPost]
        public async Task<IActionResult> GetExternalPolicyJson(string policyId)
        {
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.ComplianceManager, RoleNames.Admin, RoleNames.SuperAdmin);
            if (unauthorized != null) return unauthorized;

            // API endpoint for AJAX calls to get specific policy details
            var externalPolicies = await _complianceService.GetExternalPolicyReferencesAsync("");
            var policy = externalPolicies.FirstOrDefault(p => p.PolicyTitle.Contains(policyId));
            
            return Json(policy);
        }
    }
}
