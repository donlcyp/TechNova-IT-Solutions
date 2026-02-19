using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class DashboardModel : PageModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public IActionResult OnGet()
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

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "superadmin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Super Administrator";

            return Page();
        }
    }
}



