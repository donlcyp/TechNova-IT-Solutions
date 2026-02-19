using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.Supplier
{
    public class ProcurementModel : PageModel
    {
        public IActionResult OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            return Page();
        }
    }
}
