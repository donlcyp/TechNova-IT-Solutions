using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;

namespace TechNova_IT_Solutions.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public List<ComplianceReportItem> ComplianceReportData { get; set; } = new();
        public List<SupplierReportItem> SupplierReportData { get; set; } = new();
        public List<ProcurementReportItem> ProcurementReportData { get; set; } = new();

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

            // Fetch compliance report data
            var complianceData = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.User != null && pa.User.Role == "Employee")
                .ToListAsync();

            ComplianceReportData = complianceData
                .Select(pa => new ComplianceReportItem
                {
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    AssignedPolicy = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    ComplianceStatus = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged" 
                        ? "Compliant" 
                        : "Non-Compliant",
                    AcknowledgedDate = pa.ComplianceStatus != null && pa.ComplianceStatus.AcknowledgedDate != null
                        ? pa.ComplianceStatus.AcknowledgedDate.Value.ToString("MMM dd, yyyy")
                        : "Not Acknowledged"
                })
                .OrderBy(x => x.EmployeeName)
                .ToList();

            // Fetch supplier compliance report data
            SupplierReportData = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .OrderBy(sp => sp.Supplier.SupplierName)
                .Select(sp => new SupplierReportItem
                {
                    SupplierName = sp.Supplier != null ? sp.Supplier.SupplierName : "Unknown",
                    AssignedPolicy = sp.Policy != null ? sp.Policy.PolicyTitle : "Unknown",
                    ComplianceStatus = sp.ComplianceStatus ?? "Pending",
                    AssignedDate = sp.AssignedDate != null 
                        ? sp.AssignedDate.Value.ToString("MMM dd, yyyy")
                        : "N/A"
                })
                .ToListAsync();

            // Fetch procurement report data
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            ProcurementReportData = await _context.Procurements
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new ProcurementReportItem
                {
                    ProcurementId = "PRO-" + p.ProcurementId.ToString("D3"),
                    ItemName = p.ItemName ?? "Unknown",
                    Supplier = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle : "General",
                    PurchaseDate = p.PurchaseDate != null
                        ? p.PurchaseDate.Value.ToString("MMM dd, yyyy")
                        : "N/A",
                    ApprovalStatus = p.PurchaseDate >= oneWeekAgo ? "Pending" : "Approved"
                })
                .ToListAsync();

            return Page();
        }
    }

    public class ComplianceReportItem
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public string AcknowledgedDate { get; set; } = string.Empty;
    }

    public class SupplierReportItem
    {
        public string SupplierName { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public string AssignedDate { get; set; } = string.Empty;
    }

    public class ProcurementReportItem
    {
        public string ProcurementId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string LinkedPolicy { get; set; } = string.Empty;
        public string PurchaseDate { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
    }
}
