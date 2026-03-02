using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IComplianceManagerService _complianceManagerService;

        public AdminController(
            IAdminService adminService,
            IUserService userService,
            ApplicationDbContext context,
            IComplianceManagerService complianceManagerService)
        {
            _adminService = adminService;
            _userService = userService;
            _context = context;
            _complianceManagerService = complianceManagerService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            int? branchId = null;
            var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
            if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var parsedId))
                branchId = parsedId;

            var data = await _adminService.GetDashboardDataAsync(branchId);
            return View(data);
        }

        public async Task<IActionResult> UserManagement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            var allUsers = await _userService.GetAllUsersAsync();

            // Branch Admins only see users in their branch
            if (userRole != RoleNames.SuperAdmin)
            {
                int? callerBranchId = null;
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var parsedId))
                    callerBranchId = parsedId;

                if (callerBranchId.HasValue)
                    allUsers = allUsers.Where(u => u.BranchId == callerBranchId.Value).ToList();
            }

            return View(allUsers);
        }

        public IActionResult PolicyManagement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult SupplierManagement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult ComplianceMonitoring()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult AuditLogs()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public async Task<IActionResult> PolicyArchives(string? searchTerm, string? categoryFilter)
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var archiveData = await _complianceManagerService.GetPolicyArchivesAsync(searchTerm, categoryFilter);

            var model = new TechNova_IT_Solutions.Pages.ChiefComplianceManager.PolicyArchivesModel(_complianceManagerService, _context)
            {
                SearchTerm = searchTerm,
                CategoryFilter = categoryFilter,
                TotalArchived = archiveData.TotalArchived,
                ArchivedThisMonth = archiveData.ArchivedThisMonth,
                TotalCategories = archiveData.TotalCategories,
                ArchivedPolicies = archiveData.ArchivedPolicies
            };

            model.ExternalPolicyImports = await _context.ExternalPolicyImports
                .AsNoTracking()
                .OrderByDescending(i => i.ImportedAt)
                .Take(50)
                .Select(i => new TechNova_IT_Solutions.Pages.ChiefComplianceManager.PolicyArchivesModel.ExternalPolicyArchiveRow
                {
                    ImportId = i.ImportId,
                    PolicyTitle = i.PolicyTitle,
                    Category = i.Category ?? "General",
                    SourceApi = i.SourceApi,
                    DocumentNumber = i.DocumentNumber,
                    ReviewStatus = i.ReviewStatus,
                    ImportedAt = i.ImportedAt,
                    ReviewedAt = i.ReviewedAt
                })
                .ToListAsync();
            model.ExternalImportsCount = model.ExternalPolicyImports.Count;

            return View("~/Pages/Admin/PolicyArchives.cshtml", model);
        }

        public IActionResult Reports()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult Procurement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.BranchAdmin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public async Task<IActionResult> ExternalPolicyReferences(string category = "all")
        {
            // External policy references: Super Admin oversight only (Compliance Manager has their own page)
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            int? branchId = null;
            var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
            if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var parsedBranchId))
                branchId = parsedBranchId;

            var externalPolicies = await _complianceManagerService.GetExternalPolicyReferencesAsync(category);

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
                .Where(u => u.Role == RoleNames.Employee && u.Status == "Active" &&
                    (!branchId.HasValue || u.BranchId == branchId))
                .OrderBy(u => u.FirstName)
                .Select(u => new { u.UserId, Name = (u.FirstName + " " + u.LastName).Trim() })
                .ToListAsync();

            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Status == "Active" &&
                    (!branchId.HasValue || s.BranchId == branchId || s.BranchId == null))
                .OrderBy(s => s.SupplierName)
                .Select(s => new { s.SupplierId, Name = s.SupplierName ?? string.Empty })
                .ToListAsync();

            ViewBag.Category = category;
            ViewBag.StagedImports = stagedImports;
            ViewBag.ActiveEmployees = employees;
            ViewBag.ActiveSuppliers = suppliers;
            ViewBag.PendingImportCount = pendingCount;
            ViewBag.ApprovedImportCount = approvedCount;
            ViewBag.RejectedImportCount = rejectedCount;

            return View("~/Pages/Admin/ExternalPolicyReferences.cshtml", externalPolicies);
        }
    }
}
