using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Constants;

namespace TechNova_IT_Solutions.Pages.Admin
{
    public class BranchComplianceModel : PageModel
    {
        public string BranchName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public bool IsGlobalRole { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            var role = HttpContext.Session.GetString(SessionKeys.UserRole) ?? string.Empty;

            var allowed = new[]
            {
                RoleNames.Admin, RoleNames.SuperAdmin,
                RoleNames.ComplianceManager, RoleNames.ChiefComplianceManager
            };

            if (!allowed.Contains(role))
            {
                return role == RoleNames.Employee
                    ? RedirectToPage("/Employee/Dashboard")
                    : RedirectToPage("/Account/Login");
            }

            UserRole = role;
            IsGlobalRole = role == RoleNames.SuperAdmin || role == RoleNames.ChiefComplianceManager;
            BranchName = HttpContext.Session.GetString(SessionKeys.BranchName) ?? "All Branches";

            return Page();
        }
    }
}
