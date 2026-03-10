using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.Supplier
{
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string ReportType { get; set; } = "compliance";

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

        public int TotalRecords { get; set; }
        public int CompliantCount { get; set; }
        public int PendingCount { get; set; }
        public string ComplianceRate { get; set; } = "0";

        public List<SupplierComplianceReportItem> ComplianceData { get; set; } = new();
        public List<SupplierProcurementReportItem> ProcurementData { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail);
            if (string.IsNullOrWhiteSpace(userEmail))
                return RedirectToPage("/Supplier/Login");

            UserEmail = userEmail;
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Supplier";

            ReportType = string.IsNullOrWhiteSpace(ReportType) ? "compliance" : ReportType.Trim().ToLowerInvariant();

            var today = DateTime.UtcNow.Date;
            FromDate ??= today.AddDays(-30);
            ToDate ??= today;
            if (FromDate > ToDate) (FromDate, ToDate) = (ToDate, FromDate);

            GeneratedOn = DateTime.UtcNow;

            var supplier = await _context.Suppliers
                .Include(s => s.SupplierPolicies).ThenInclude(sp => sp.Policy)
                .FirstOrDefaultAsync(s => s.Email == userEmail);

            if (supplier == null)
                return Page();

            if (ReportType == "procurement")
            {
                var procurements = await _context.Procurements
                    .Include(p => p.RelatedPolicy)
                    .Where(p => p.SupplierId == supplier.SupplierId)
                    .Where(p => !FromDate.HasValue || p.PurchaseDate >= FromDate.Value)
                    .Where(p => !ToDate.HasValue || p.PurchaseDate <= ToDate.Value.AddDays(1))
                    .OrderByDescending(p => p.PurchaseDate)
                    .ToListAsync();

                ProcurementData = procurements.Select(p => new SupplierProcurementReportItem
                {
                    ItemName = p.ItemName ?? "—",
                    Quantity = p.Quantity ?? 0,
                    UnitPrice = p.OriginalAmount,
                    TotalCost = p.ConvertedAmount,
                    Status = p.Status,
                    PurchaseDate = p.PurchaseDate,
                    LinkedPolicy = p.RelatedPolicy?.PolicyTitle ?? "—"
                }).ToList();

                TotalRecords = ProcurementData.Count;
                CompliantCount = ProcurementData.Count(x => x.Status == "Approved");
                PendingCount = ProcurementData.Count(x => x.Status == "Pending");
            }
            else
            {
                var policies = supplier.SupplierPolicies
                    .Where(sp => !FromDate.HasValue || (sp.AssignedDate.HasValue && sp.AssignedDate.Value >= FromDate.Value))
                    .Where(sp => !ToDate.HasValue || (sp.AssignedDate.HasValue && sp.AssignedDate.Value <= ToDate.Value.AddDays(1)))
                    .ToList();

                ComplianceData = policies.Select(sp => new SupplierComplianceReportItem
                {
                    PolicyTitle = sp.Policy?.PolicyTitle ?? "N/A",
                    Category = sp.Policy?.Category ?? "—",
                    Status = sp.ComplianceStatus,
                    AssignedDate = sp.AssignedDate
                }).OrderByDescending(x => x.AssignedDate).ToList();

                TotalRecords = ComplianceData.Count;
                CompliantCount = ComplianceData.Count(x => string.Equals(x.Status, "Compliant", StringComparison.OrdinalIgnoreCase));
                PendingCount = ComplianceData.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            }

            ComplianceRate = TotalRecords > 0 ? ((CompliantCount * 100) / TotalRecords).ToString() : "0";

            return Page();
        }
    }

    public class SupplierComplianceReportItem
    {
        public string PolicyTitle { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? AssignedDate { get; set; }
    }

    public class SupplierProcurementReportItem
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalCost { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PurchaseDate { get; set; }
        public string LinkedPolicy { get; set; } = "—";
    }
}
