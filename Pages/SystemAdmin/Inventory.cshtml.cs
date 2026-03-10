using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;

namespace TechNova_IT_Solutions.Pages.SystemAdmin
{
    public class InventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        public int TotalItems { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int TotalProcurements { get; set; }

        public List<TechNova_IT_Solutions.Pages.BranchAdmin.InventoryItem> InventoryItems { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.SystemAdmin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.BranchAdmin) return RedirectToPage("/BranchAdmin/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager)
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? string.Empty;
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";

            // System Admin sees ALL procurements (no branch scoping)
            var procurements = await _context.Procurements
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            TotalProcurements = procurements.Count;

            InventoryItems = procurements.Select(p => new TechNova_IT_Solutions.Pages.BranchAdmin.InventoryItem
            {
                Id = p.ProcurementId,
                ItemName = p.ItemName ?? "—",
                SupplierName = p.Supplier?.SupplierName ?? "—",
                Quantity = p.Quantity ?? 0,
                UnitPrice = p.OriginalAmount,
                TotalCost = p.ConvertedAmount,
                Status = p.Status ?? "Unknown",
                ProcurementDate = p.PurchaseDate,
                LinkedPolicy = p.RelatedPolicy?.PolicyTitle ?? "—"
            }).ToList();

            TotalItems = InventoryItems.Select(i => i.ItemName).Distinct().Count();
            LowStockCount = InventoryItems.Count(i => i.Quantity > 0 && i.Quantity <= 5);
            OutOfStockCount = InventoryItems.Count(i => i.Quantity == 0);

            return Page();
        }
    }
}
