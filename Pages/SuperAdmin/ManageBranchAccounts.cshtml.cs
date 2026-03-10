using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class ManageBranchAccountsModel : PageModel
    {
        private readonly IUserService _userService;

        public ManageBranchAccountsModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<UserData> BranchUsers { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(
                this,
                new[] { RoleNames.SuperAdmin },
                new Dictionary<string, string>
                {
                    [RoleNames.ChiefComplianceManager] = "/ChiefComplianceManager/ComplianceDashboard",
                    [RoleNames.ComplianceManager]      = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.BranchAdmin]            = "/BranchAdmin/Dashboard",
                    [RoleNames.Employee]               = "/Employee/Dashboard",
                    [RoleNames.Supplier]               = "/Supplier/Dashboard"
                });
            if (denied != null) return denied;

            var allUsers = await _userService.GetAllUsersAsync();
            BranchUsers = allUsers
                .Where(u => u.Role == RoleNames.BranchAdmin ||
                            u.Role == RoleNames.ComplianceManager ||
                            u.Role == RoleNames.Employee)
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FirstName)
                .ToList();

            return Page();
        }
    }
}
