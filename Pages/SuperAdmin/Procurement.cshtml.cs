using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class ProcurementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProcurementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ProcurementRow> Rows { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.SuperAdmin });
            if (denied != null) return denied;

            Rows = await _context.Procurements
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(200)
                .Select(p => new ProcurementRow
                {
                    ProcurementId = p.ProcurementId,
                    ItemName = p.ItemName ?? "N/A",
                    Supplier = p.Supplier != null ? p.Supplier.SupplierName ?? "N/A" : "N/A",
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle ?? "N/A" : "General",
                    PurchaseDate = p.PurchaseDate
                })
                .ToListAsync();

            return Page();
        }
    }

    public class ProcurementRow
    {
        public int ProcurementId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string LinkedPolicy { get; set; } = string.Empty;
        public DateTime? PurchaseDate { get; set; }
    }
}



