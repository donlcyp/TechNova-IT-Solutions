using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Constants;

namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class BranchComplianceArchiveModel : PageModel
    {
        public string BranchName { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            var role = HttpContext.Session.GetString(SessionKeys.UserRole) ?? string.Empty;
            if (role != RoleNames.ComplianceManager && role != RoleNames.SuperAdmin)
                return RedirectToPage("/Account/Login");

            BranchName = HttpContext.Session.GetString(SessionKeys.BranchName) ?? string.Empty;
            return Page();
        }
    }
}
