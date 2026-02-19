using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class ResetDeactivateAccountsModel : PageModel
    {
        private readonly IUserService _userService;

        public ResetDeactivateAccountsModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<UserData> Users { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(
                this,
                new[] { RoleNames.SuperAdmin },
                new Dictionary<string, string>
                {
                    [RoleNames.ComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.Employee] = "/Employee/Dashboard",
                    [RoleNames.Supplier] = "/Supplier/Dashboard"
                });
            if (denied != null) return denied;

            Users = await _userService.GetAllUsersAsync();
            return Page();
        }
    }
}



