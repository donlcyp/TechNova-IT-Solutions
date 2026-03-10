using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.Supplier
{
    public class InventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public List<InventoryItemVm> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier, RoleNames.SuperAdmin }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            var userRole  = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole == RoleNames.SuperAdmin)
                return Page();

            var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail);
            if (string.IsNullOrWhiteSpace(userEmail))
                return RedirectToPage("/Supplier/Login");

            var supplier = await _context.Suppliers
                .Include(s => s.SupplierItems)
                .FirstOrDefaultAsync(s => s.Email == userEmail);

            if (supplier == null)
                return RedirectToPage("/Account/AccessDenied");

            SupplierId = supplier.SupplierId;
            SupplierName = supplier.SupplierName ?? string.Empty;
            Items = supplier.SupplierItems
                .OrderBy(i => i.ItemName)
                .Select(i => new InventoryItemVm
                {
                    SupplierItemId = i.SupplierItemId,
                    ItemName = i.ItemName,
                    Category = i.Category ?? string.Empty,
                    UnitPrice = i.UnitPrice,
                    CurrencyCode = i.CurrencyCode,
                    QuantityAvailable = i.QuantityAvailable,
                    Status = i.Status,
                    LastUpdated = i.LastUpdated
                })
                .ToList();

            return Page();
        }
    }

    public class InventoryItemVm
    {
        public int SupplierItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = "PHP";
        public int QuantityAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}
