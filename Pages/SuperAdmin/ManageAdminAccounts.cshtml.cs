using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class ManageAdminAccountsModel : PageModel
    {
        private readonly IUserService _userService;

        public ManageAdminAccountsModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<UserData> AdminUsers { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(
                this,
                new[] { RoleNames.SuperAdmin },
                new Dictionary<string, string>
                {
                    [RoleNames.ChiefComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.ComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.Employee] = "/Employee/Dashboard",
                    [RoleNames.Supplier] = "/Supplier/Dashboard"
                });
            if (denied != null) return denied;

            var allUsers = await _userService.GetAllUsersAsync();
            AdminUsers = allUsers
                .Where(u => RoleNames.IsAdminRole(u.Role) || u.Role == RoleNames.ChiefComplianceManager || u.Role == RoleNames.SuperAdmin)
                .OrderByDescending(u => u.Role == RoleNames.SuperAdmin)
                .ThenByDescending(u => u.Role == RoleNames.ChiefComplianceManager)
                .ThenBy(u => u.FirstName)
                .ToList();

            return Page();
        }
    }
}



