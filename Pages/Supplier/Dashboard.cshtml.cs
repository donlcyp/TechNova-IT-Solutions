using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.Supplier
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string SupplierName { get; set; } = string.Empty;
        public int PendingCompliance { get; set; }
        public int CompletedCompliance { get; set; }
        public int PendingProcurements { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        
        // Policies assigned to this supplier
        public List<SupplierPolicyItem> AssignedPolicies { get; set; } = new();
        public List<SupplierDashboardProcurementItem> RecentProcurements { get; set; } = new();
        public List<SupplierStockAlertItem> LowStockItems { get; set; } = new();
        public List<SupplierStockAlertItem> OutOfStockItems { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier, RoleNames.SuperAdmin }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail);
            var userRole  = HttpContext.Session.GetString(SessionKeys.UserRole);

            // SuperAdmin override: show a generic supplier view without a specific supplier record
            if (userRole == RoleNames.SuperAdmin)
            {
                SupplierName = "Super Admin (Override)";
                return Page();
            }

            // Find the Supplier record linked to this User (via Email)
            var supplier = await _context.Suppliers
                .Include(s => s.SupplierPolicies)
                .ThenInclude(sp => sp.Policy)
                .FirstOrDefaultAsync(s => s.Email == userEmail);

            if (supplier == null)
            {
                // Should not happen if data consistency is maintained
                return RedirectToPage("/Account/AccessDenied");
            }

            SupplierName = supplier.SupplierName ?? string.Empty;

            // Calculate stats
            PendingCompliance = supplier.SupplierPolicies.Count(sp => sp.ComplianceStatus == "Pending" || sp.ComplianceStatus == "Non-Compliant");
            CompletedCompliance = supplier.SupplierPolicies.Count(sp => sp.ComplianceStatus == "Compliant");

            // Get policies
            AssignedPolicies = supplier.SupplierPolicies
                .Select(sp => new SupplierPolicyItem
                {
                    SupplierPolicyId = sp.SupplierPolicyId,
                    PolicyTitle = sp.Policy.PolicyTitle ?? string.Empty,
                    AssignedDate = sp.AssignedDate ?? DateTime.MinValue,
                    Status = sp.ComplianceStatus,
                    Description = sp.Policy.Description ?? string.Empty
                })
                .OrderByDescending(p => p.AssignedDate)
                .ToList();

            RecentProcurements = await _context.Procurements
                .Where(p => p.SupplierId == supplier.SupplierId)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(5)
                .Select(p => new SupplierDashboardProcurementItem
                {
                    ProcurementCode = "PROC-" + p.ProcurementId.ToString("D3"),
                    ItemName = p.ItemName ?? string.Empty,
                    Quantity = p.Quantity ?? 0,
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle : "General",
                    OrderDate = p.PurchaseDate ?? DateTime.Now,
                    Status = string.IsNullOrWhiteSpace(p.Status) ? ProcurementStatuses.Draft : p.Status
                })
                .ToListAsync();

            PendingProcurements = RecentProcurements.Count(p =>
                string.Equals(p.Status, ProcurementStatuses.Submitted, StringComparison.OrdinalIgnoreCase));

            var supplierItems = await _context.SupplierItems
                .Where(i => i.SupplierId == supplier.SupplierId)
                .OrderBy(i => i.ItemName)
                .Select(i => new SupplierStockAlertItem
                {
                    ItemName = i.ItemName,
                    QuantityAvailable = i.QuantityAvailable,
                    Status = i.Status
                })
                .ToListAsync();

            LowStockItems = supplierItems
                .Where(i => i.QuantityAvailable > 0 && i.QuantityAvailable <= 10)
                .Take(5)
                .ToList();

            OutOfStockItems = supplierItems
                .Where(i => i.QuantityAvailable <= 0 || string.Equals(i.Status, "OutOfStock", StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            LowStockCount = supplierItems.Count(i => i.QuantityAvailable > 0 && i.QuantityAvailable <= 10);
            OutOfStockCount = supplierItems.Count(i => i.QuantityAvailable <= 0 || string.Equals(i.Status, "OutOfStock", StringComparison.OrdinalIgnoreCase));

            return Page();
        }

        public async Task<IActionResult> OnPostAcknowledge(int supplierPolicyId)
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail);
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToPage("/Supplier/Login");
            }

            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Email == userEmail);

            if (supplier == null)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            var supplierPolicy = await _context.SupplierPolicies
                .FirstOrDefaultAsync(sp => sp.SupplierPolicyId == supplierPolicyId && sp.SupplierId == supplier.SupplierId);

            if (supplierPolicy == null)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            if (!string.Equals(supplierPolicy.ComplianceStatus, "Compliant", StringComparison.OrdinalIgnoreCase))
            {
                supplierPolicy.ComplianceStatus = "Compliant";
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }

    public class SupplierPolicyItem
    {
        public int SupplierPolicyId { get; set; }
        public string PolicyTitle { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SupplierDashboardProcurementItem
    {
        public string ProcurementCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string LinkedPolicy { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SupplierStockAlertItem
    {
        public string ItemName { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}



