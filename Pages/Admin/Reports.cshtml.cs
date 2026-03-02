using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages
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

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string GeneratedByRole { get; set; } = string.Empty;
        public int? CallerBranchId { get; set; }
        public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

        public int TotalRecords { get; set; }
        public int CompliantCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal ComplianceRate { get; set; }
        public int TotalQuantity { get; set; }

        public List<ComplianceReportItem> ComplianceReportData { get; set; } = new();
        public List<SupplierReportItem> SupplierReportData { get; set; } = new();
        public List<ProcurementReportItem> ProcurementReportData { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var denied = RequireAuthorizedReportUser();
            if (denied != null)
            {
                return denied;
            }

            InitializeReportContext();
            await LoadReportDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetExportCsvAsync(string reportType, DateTime? fromDate, DateTime? toDate)
        {
            var denied = RequireAuthorizedReportUser();
            if (denied != null)
            {
                return denied;
            }

            ReportType = string.IsNullOrWhiteSpace(reportType) ? "compliance" : reportType.Trim().ToLowerInvariant();
            FromDate = fromDate;
            ToDate = toDate;

            InitializeReportContext();
            await LoadReportDataAsync();

            var csv = BuildCsv();
            var fileName = $"{ReportType}-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        private IActionResult? RequireAuthorizedReportUser()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (!RoleNames.IsAdminRole(userRole) && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            GeneratedByRole = userRole ?? RoleNames.BranchAdmin;

            // Branch scoping: Branch Admins only see their branch reports; SuperAdmin sees all
            if (RoleNames.IsAdminRole(userRole))
            {
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var bid))
                {
                    CallerBranchId = bid;
                }
            }

            return null;
        }

        private void InitializeReportContext()
        {
            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";

            ReportType = string.IsNullOrWhiteSpace(ReportType)
                ? "compliance"
                : ReportType.Trim().ToLowerInvariant();

            var today = DateTime.UtcNow.Date;
            FromDate ??= today.AddDays(-30);
            ToDate ??= today;

            if (FromDate > ToDate)
            {
                (FromDate, ToDate) = (ToDate, FromDate);
            }
        }

        private async Task LoadReportDataAsync()
        {
            switch (ReportType)
            {
                case "supplier":
                    await LoadSupplierReportAsync();
                    break;
                case "procurement":
                    await LoadProcurementReportAsync();
                    break;
                default:
                    ReportType = "compliance";
                    await LoadComplianceReportAsync();
                    break;
            }
        }

        private async Task LoadComplianceReportAsync()
        {
            var startDate = FromDate!.Value.Date;
            var endDate = ToDate!.Value.Date.AddDays(1);
            bool scoped = CallerBranchId.HasValue;

            ComplianceReportData = await _context.PolicyAssignments
                .AsNoTracking()
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.User != null && pa.User.Role == RoleNames.Employee)
                .Where(pa => !scoped || (pa.User != null && pa.User.BranchId == CallerBranchId))
                .Where(pa => pa.AssignedDate.HasValue
                    && pa.AssignedDate.Value >= startDate
                    && pa.AssignedDate.Value < endDate)
                .OrderBy(pa => pa.User!.FirstName)
                .ThenBy(pa => pa.User!.LastName)
                .Select(pa => new ComplianceReportItem
                {
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    AssignedPolicy = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    ComplianceStatus = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged"
                        ? "Compliant"
                        : "Pending",
                    AssignedDate = pa.AssignedDate,
                    AcknowledgedDate = pa.ComplianceStatus != null ? pa.ComplianceStatus.AcknowledgedDate : null
                })
                .ToListAsync();

            TotalRecords = ComplianceReportData.Count;
            CompliantCount = ComplianceReportData.Count(x => x.ComplianceStatus == "Compliant");
            PendingCount = ComplianceReportData.Count - CompliantCount;
            ComplianceRate = TotalRecords == 0
                ? 0
                : Math.Round((decimal)CompliantCount * 100 / TotalRecords, 1);
        }

        private async Task LoadSupplierReportAsync()
        {
            var startDate = FromDate!.Value.Date;
            var endDate = ToDate!.Value.Date.AddDays(1);
            bool scoped = CallerBranchId.HasValue;

            SupplierReportData = await _context.SupplierPolicies
                .AsNoTracking()
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .Where(sp => !scoped || sp.Supplier == null || sp.Supplier.BranchId == CallerBranchId || sp.Supplier.BranchId == null)
                .Where(sp => sp.AssignedDate.HasValue
                    && sp.AssignedDate.Value >= startDate
                    && sp.AssignedDate.Value < endDate)
                .OrderBy(sp => sp.Supplier.SupplierName)
                .Select(sp => new SupplierReportItem
                {
                    SupplierName = sp.Supplier != null ? sp.Supplier.SupplierName : "Unknown",
                    AssignedPolicy = sp.Policy != null ? sp.Policy.PolicyTitle : "Unknown",
                    ComplianceStatus = string.IsNullOrWhiteSpace(sp.ComplianceStatus) ? "Pending" : sp.ComplianceStatus,
                    AssignedDate = sp.AssignedDate
                })
                .ToListAsync();

            TotalRecords = SupplierReportData.Count;
            CompliantCount = SupplierReportData.Count(x => x.ComplianceStatus.Equals("Compliant", StringComparison.OrdinalIgnoreCase));
            PendingCount = SupplierReportData.Count(x => x.ComplianceStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase));
            RejectedCount = SupplierReportData.Count(x => x.ComplianceStatus.Equals("Non-Compliant", StringComparison.OrdinalIgnoreCase));
            ComplianceRate = TotalRecords == 0
                ? 0
                : Math.Round((decimal)CompliantCount * 100 / TotalRecords, 1);
        }

        private async Task LoadProcurementReportAsync()
        {
            var startDate = FromDate!.Value.Date;
            var endDate = ToDate!.Value.Date.AddDays(1);
            bool scoped = CallerBranchId.HasValue;

            ProcurementReportData = await _context.Procurements
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .Where(p => !scoped || p.Supplier == null || p.Supplier.BranchId == CallerBranchId || p.Supplier.BranchId == null)
                .Where(p => p.PurchaseDate.HasValue
                    && p.PurchaseDate.Value >= startDate
                    && p.PurchaseDate.Value < endDate)
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new ProcurementReportItem
                {
                    ProcurementId = $"PRO-{p.ProcurementId:D3}",
                    ItemName = p.ItemName ?? "Unknown",
                    Supplier = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle : "General",
                    PurchaseDate = p.PurchaseDate,
                    Quantity = p.Quantity ?? 0,
                    ApprovalStatus = NormalizeProcurementStatus(p.Status)
                })
                .ToListAsync();

            TotalRecords = ProcurementReportData.Count;
            CompliantCount = ProcurementReportData.Count(x => x.ApprovalStatus == "Approved");
            PendingCount = ProcurementReportData.Count(x => x.ApprovalStatus == "Pending");
            RejectedCount = ProcurementReportData.Count(x => x.ApprovalStatus == "Rejected");
            TotalQuantity = ProcurementReportData.Sum(x => x.Quantity);
            ComplianceRate = TotalRecords == 0
                ? 0
                : Math.Round((decimal)CompliantCount * 100 / TotalRecords, 1);
        }

        private string BuildCsv()
        {
            var builder = new StringBuilder();

            if (ReportType == "supplier")
            {
                builder.AppendLine("Supplier Name,Assigned Policy,Compliance Status,Assigned Date");
                foreach (var row in SupplierReportData)
                {
                    builder.AppendLine(string.Join(",",
                        EscapeCsv(row.SupplierName),
                        EscapeCsv(row.AssignedPolicy),
                        EscapeCsv(row.ComplianceStatus),
                        EscapeCsv(row.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A")));
                }
            }
            else if (ReportType == "procurement")
            {
                builder.AppendLine("Procurement ID,Item Name,Supplier,Linked Policy,Purchase Date,Quantity,Status");
                foreach (var row in ProcurementReportData)
                {
                    builder.AppendLine(string.Join(",",
                        EscapeCsv(row.ProcurementId),
                        EscapeCsv(row.ItemName),
                        EscapeCsv(row.Supplier),
                        EscapeCsv(row.LinkedPolicy),
                        EscapeCsv(row.PurchaseDate?.ToString("yyyy-MM-dd") ?? "N/A"),
                        row.Quantity.ToString(),
                        EscapeCsv(row.ApprovalStatus)));
                }
            }
            else
            {
                builder.AppendLine("Employee Name,Assigned Policy,Compliance Status,Assigned Date,Acknowledged Date");
                foreach (var row in ComplianceReportData)
                {
                    builder.AppendLine(string.Join(",",
                        EscapeCsv(row.EmployeeName),
                        EscapeCsv(row.AssignedPolicy),
                        EscapeCsv(row.ComplianceStatus),
                        EscapeCsv(row.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A"),
                        EscapeCsv(row.AcknowledgedDate?.ToString("yyyy-MM-dd") ?? "N/A")));
                }
            }

            return builder.ToString();
        }

        private static string NormalizeProcurementStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "Pending";
            }

            if (status.Contains("reject", StringComparison.OrdinalIgnoreCase))
            {
                return "Rejected";
            }

            if (status.Contains("approve", StringComparison.OrdinalIgnoreCase))
            {
                return "Approved";
            }

            return "Pending";
        }

        private static string EscapeCsv(string? value)
        {
            var normalized = value ?? string.Empty;
            if (normalized.Contains(',') || normalized.Contains('"') || normalized.Contains('\n'))
            {
                return $"\"{normalized.Replace("\"", "\"\"")}\"";
            }

            return normalized;
        }
    }

    public class ComplianceReportItem
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime? AssignedDate { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
    }

    public class SupplierReportItem
    {
        public string SupplierName { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime? AssignedDate { get; set; }
    }

    public class ProcurementReportItem
    {
        public string ProcurementId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string LinkedPolicy { get; set; } = string.Empty;
        public DateTime? PurchaseDate { get; set; }
        public int Quantity { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
    }
}
