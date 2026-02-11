using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;

namespace TechNova_IT_Solutions.Pages
{
    public class ProcurementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProcurementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // Summary Data
        public int TotalProcurements { get; set; }
        public int PendingApprovals { get; set; }
        public int ProcurementsThisMonth { get; set; }

        // Procurement Records
        public List<ProcurementRecord> ProcurementRecords { get; set; } = new();

        // Reference Data
        public List<SupplierReference> Suppliers { get; set; } = new();
        public List<PolicyReference> Policies { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check authentication
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Admin can access
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                if (userRole == "Employee") return RedirectToPage("/Employee/Dashboard");
                if (userRole == "ComplianceManager") return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString("UserEmail") ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            // Calculate summary statistics
            TotalProcurements = await _context.Procurements.CountAsync();
            
            // For this demo, assume recent ones are pending
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            PendingApprovals = await _context.Procurements
                .Where(p => p.PurchaseDate >= oneWeekAgo)
                .CountAsync();
            
            // Count procurements this month
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ProcurementsThisMonth = await _context.Procurements
                .Where(p => p.PurchaseDate >= firstDayOfMonth)
                .CountAsync();

            // Fetch procurement records from database
            ProcurementRecords = await _context.Procurements
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new ProcurementRecord
                {
                    ProcurementId = "PROC-" + p.ProcurementId.ToString("D3"),
                    ItemName = p.ItemName ?? string.Empty,
                    Category = p.Category ?? string.Empty,
                    Quantity = p.Quantity ?? 0,
                    SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle : "General",
                    PurchaseDate = p.PurchaseDate ?? DateTime.Now,
                    ApprovalStatus = p.PurchaseDate >= oneWeekAgo ? "Pending" : "Approved"
                })
                .ToListAsync();

            // Fetch suppliers for dropdown
            Suppliers = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .Select(s => new SupplierReference
                {
                    Id = s.SupplierId,
                    Name = s.SupplierName ?? string.Empty
                })
                .ToListAsync();

            // Fetch policies for dropdown
            Policies = await _context.Policies
                .OrderBy(p => p.PolicyTitle)
                .Select(p => new PolicyReference
                {
                    Id = p.PolicyId,
                    Title = p.PolicyTitle ?? string.Empty
                })
                .ToListAsync();

            return Page();
        }
    }

    public class ProcurementRecord
    {
        public string ProcurementId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string LinkedPolicy { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
    }

    public class SupplierReference
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PolicyReference
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
