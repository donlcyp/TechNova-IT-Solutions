using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.Supplier
{
    public class ComplianceStatusModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ComplianceStatusModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<SupplierComplianceItem> Policies { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail);
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToPage("/Supplier/Login");
            }

            var supplier = await _context.Suppliers
                .Include(s => s.SupplierPolicies)
                .ThenInclude(sp => sp.Policy)
                .FirstOrDefaultAsync(s => s.Email == userEmail);

            if (supplier == null)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            Policies = supplier.SupplierPolicies
                .Select(sp => new SupplierComplianceItem
                {
                    PolicyTitle = sp.Policy.PolicyTitle,
                    AssignedDate = sp.AssignedDate ?? DateTime.MinValue,
                    Status = sp.ComplianceStatus
                })
                .OrderByDescending(p => p.AssignedDate)
                .ToList();

            return Page();
        }
    }

    public class SupplierComplianceItem
    {
        public string PolicyTitle { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}



