using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TechNova_IT_Solutions.Controllers
{
    public class ComplianceManagerController : Controller
    {
        private readonly IComplianceManagerService _complianceService;
        private readonly ApplicationDbContext _context;

        public ComplianceManagerController(IComplianceManagerService complianceService, ApplicationDbContext context)
        {
            _complianceService = complianceService;
            _context = context;
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

            var stagedImports = await _context.ExternalPolicyImports
                .AsNoTracking()
                .OrderByDescending(i => i.ImportedAt)
                .Take(80)
                .Select(i => new
                {
                    i.ImportId,
                    i.PolicyTitle,
                    i.Category,
                    i.DocumentNumber,
                    i.ExternalUrl,
                    i.ReviewStatus,
                    i.ImportedAt,
                    i.ReviewedAt,
                    i.ReviewNotes,
                    i.ApprovedPolicyId
                })
                .ToListAsync();

            var pendingCount = stagedImports.Count(i => i.ReviewStatus == "PendingReview");
            var approvedCount = stagedImports.Count(i => i.ReviewStatus == "Approved");
            var rejectedCount = stagedImports.Count(i => i.ReviewStatus == "Rejected");

            var employees = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == RoleNames.Employee && u.Status == "Active")
                .OrderBy(u => u.FirstName)
                .Select(u => new { u.UserId, Name = (u.FirstName + " " + u.LastName).Trim() })
                .ToListAsync();

            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .Select(s => new { s.SupplierId, Name = s.SupplierName ?? string.Empty })
                .ToListAsync();
            
            ViewBag.Category = category;
            ViewBag.UserRole = userRole;
            ViewBag.StagedImports = stagedImports;
            ViewBag.ActiveEmployees = employees;
            ViewBag.ActiveSuppliers = suppliers;
            ViewBag.PendingImportCount = pendingCount;
            ViewBag.ApprovedImportCount = approvedCount;
            ViewBag.RejectedImportCount = rejectedCount;
            return View("~/Pages/ComplianceManager/ExternalPolicyReferences.cshtml", externalPolicies);
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
