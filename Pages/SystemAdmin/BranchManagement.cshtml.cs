using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.SystemAdmin
{
    public class BranchManagementModel : PageModel
    {
        private readonly IBranchService _branchService;

        public BranchManagementModel(IBranchService branchService)
        {
            _branchService = branchService;
        }

        public List<BranchData> Branches { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(
                this,
                new[] { RoleNames.SystemAdmin },
                new Dictionary<string, string>
                {
                    [RoleNames.SuperAdmin]             = "/SuperAdmin/Dashboard",
                    [RoleNames.BranchAdmin]            = "/BranchAdmin/Dashboard",
                    [RoleNames.ChiefComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.ComplianceManager]      = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.Employee]               = "/Employee/Dashboard",
                    [RoleNames.Supplier]               = "/Supplier/Dashboard"
                });
            if (denied != null) return denied;

            Branches = await _branchService.GetAllBranchesAsync();
            return Page();
        }
    }
}
