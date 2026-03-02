using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TechNova_IT_Solutions.Controllers
{
    public class SystemAdminController : Controller
    {
        private readonly IComplianceManagerService _complianceManagerService;
        private readonly ApplicationDbContext _context;

        public SystemAdminController(
            IComplianceManagerService complianceManagerService,
            ApplicationDbContext context)
        {
            _complianceManagerService = complianceManagerService;
            _context = context;
        }

        public async Task<IActionResult> PolicyArchives(string? searchTerm, string? categoryFilter)
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.SystemAdmin, RoleNames.SuperAdmin);
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

            return View("~/Pages/SystemAdmin/PolicyArchives.cshtml", model);
        }
    }
}
