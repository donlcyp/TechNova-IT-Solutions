using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class ComplianceReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IComplianceManagerService _complianceService;

        public ComplianceReportsModel(ApplicationDbContext context, IComplianceManagerService complianceService)
        {
            _context = context;
            _complianceService = complianceService;
        }

        [BindProperty(SupportsGet = true)]
        public string ReportType { get; set; } = "employee";

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        public string GeneratedByName { get; set; } = string.Empty;
        public string GeneratedByRole { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

        public int TotalRecords { get; set; }
        public int CompliantCount { get; set; }
        public int PendingCount { get; set; }
        public int NonCompliantCount { get; set; }
        public decimal ComplianceRate { get; set; }

        public int TotalPolicies { get; set; }
        public int TotalEmployees { get; set; }

        public List<EmployeeComplianceReportRow> EmployeeRows { get; set; } = new();
        public List<SupplierComplianceReportRow> SupplierRows { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var denied = RequireAuthorizedReportUser();
            if (denied != null)
            {
                return denied;
            }

            InitializeReportContext();
            await LoadSharedSummaryAsync();
            await LoadReportRowsAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetExportCsvAsync(string reportType, DateTime? dateFrom, DateTime? dateTo)
        {
            var denied = RequireAuthorizedReportUser();
            if (denied != null)
            {
                return denied;
            }

            ReportType = string.IsNullOrWhiteSpace(reportType) ? "employee" : reportType.Trim().ToLowerInvariant();
            DateFrom = dateFrom;
            DateTo = dateTo;

            InitializeReportContext();
            await LoadSharedSummaryAsync();
            await LoadReportRowsAsync();

            var csv = BuildCsv();
            var fileName = $"{ReportType}-compliance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
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
            if (userRole != RoleNames.ComplianceManager && userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }

                return RedirectToPage("/Account/Login");
            }

            GeneratedByRole = userRole ?? RoleNames.ComplianceManager;
            return null;
        }

        private void InitializeReportContext()
        {
            GeneratedByName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Compliance Manager";
            ReportType = string.IsNullOrWhiteSpace(ReportType)
                ? "employee"
                : ReportType.Trim().ToLowerInvariant();

            var today = DateTime.UtcNow.Date;
            DateFrom ??= today.AddDays(-30);
            DateTo ??= today;
            if (DateFrom > DateTo)
            {
                (DateFrom, DateTo) = (DateTo, DateFrom);
            }
        }

        private async Task LoadSharedSummaryAsync()
        {
            var reportsData = await _complianceService.GetComplianceReportsDataAsync();
            TotalPolicies = reportsData.TotalPolicies;
            TotalEmployees = reportsData.TotalEmployees;
        }

        private async Task LoadReportRowsAsync()
        {
            if (ReportType == "supplier")
            {
                await LoadSupplierRowsAsync();
                return;
            }

            ReportType = "employee";
            await LoadEmployeeRowsAsync();
        }

        private async Task LoadEmployeeRowsAsync()
        {
            var startDate = DateFrom!.Value.Date;
            var endDate = DateTo!.Value.Date.AddDays(1);

            EmployeeRows = await _context.PolicyAssignments
                .AsNoTracking()
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.User != null && pa.User.Role == RoleNames.Employee)
                .Where(pa => pa.AssignedDate.HasValue
                    && pa.AssignedDate.Value >= startDate
                    && pa.AssignedDate.Value < endDate)
                .OrderBy(pa => pa.User!.FirstName)
                .ThenBy(pa => pa.User!.LastName)
                .Select(pa => new EmployeeComplianceReportRow
                {
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    Department = pa.User != null ? pa.User.Role : "N/A",
                    AssignedPolicy = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    Status = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged" ? "Compliant" : "Pending",
                    DateAssigned = pa.AssignedDate,
                    AcknowledgedDate = pa.ComplianceStatus != null ? pa.ComplianceStatus.AcknowledgedDate : null
                })
                .ToListAsync();

            TotalRecords = EmployeeRows.Count;
            CompliantCount = EmployeeRows.Count(r => r.Status == "Compliant");
            PendingCount = EmployeeRows.Count - CompliantCount;
            ComplianceRate = TotalRecords == 0
                ? 0
                : Math.Round((decimal)CompliantCount * 100 / TotalRecords, 1);
        }

        private async Task LoadSupplierRowsAsync()
        {
            var startDate = DateFrom!.Value.Date;
            var endDate = DateTo!.Value.Date.AddDays(1);

            SupplierRows = await _context.SupplierPolicies
                .AsNoTracking()
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .Where(sp => sp.AssignedDate.HasValue
                    && sp.AssignedDate.Value >= startDate
                    && sp.AssignedDate.Value < endDate)
                .OrderBy(sp => sp.Supplier.SupplierName)
                .Select(sp => new SupplierComplianceReportRow
                {
                    SupplierName = sp.Supplier != null ? sp.Supplier.SupplierName : "Unknown",
                    ContactPerson = sp.Supplier != null
                        ? $"{sp.Supplier.ContactPersonFirstName} {sp.Supplier.ContactPersonLastName}".Trim()
                        : "N/A",
                    AssignedPolicy = sp.Policy != null ? sp.Policy.PolicyTitle : "Unknown",
                    Status = string.IsNullOrWhiteSpace(sp.ComplianceStatus) ? "Pending" : sp.ComplianceStatus,
                    DateAssigned = sp.AssignedDate
                })
                .ToListAsync();

            TotalRecords = SupplierRows.Count;
            CompliantCount = SupplierRows.Count(r => r.Status.Equals("Compliant", StringComparison.OrdinalIgnoreCase));
            PendingCount = SupplierRows.Count(r => r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
            NonCompliantCount = SupplierRows.Count(r => r.Status.Equals("Non-Compliant", StringComparison.OrdinalIgnoreCase));
            ComplianceRate = TotalRecords == 0
                ? 0
                : Math.Round((decimal)CompliantCount * 100 / TotalRecords, 1);
        }

        private string BuildCsv()
        {
            var builder = new StringBuilder();

            if (ReportType == "supplier")
            {
                builder.AppendLine("Supplier Name,Contact Person,Assigned Policy,Compliance Status,Date Assigned");
                foreach (var row in SupplierRows)
                {
                    builder.AppendLine(string.Join(",",
                        EscapeCsv(row.SupplierName),
                        EscapeCsv(row.ContactPerson),
                        EscapeCsv(row.AssignedPolicy),
                        EscapeCsv(row.Status),
                        EscapeCsv(row.DateAssigned?.ToString("yyyy-MM-dd") ?? "N/A")));
                }
            }
            else
            {
                builder.AppendLine("Employee Name,Department,Assigned Policy,Compliance Status,Date Assigned,Acknowledged Date");
                foreach (var row in EmployeeRows)
                {
                    builder.AppendLine(string.Join(",",
                        EscapeCsv(row.EmployeeName),
                        EscapeCsv(row.Department),
                        EscapeCsv(row.AssignedPolicy),
                        EscapeCsv(row.Status),
                        EscapeCsv(row.DateAssigned?.ToString("yyyy-MM-dd") ?? "N/A"),
                        EscapeCsv(row.AcknowledgedDate?.ToString("yyyy-MM-dd") ?? "N/A")));
                }
            }

            return builder.ToString();
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

    public class EmployeeComplianceReportRow
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DateAssigned { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
    }

    public class SupplierComplianceReportRow
    {
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DateAssigned { get; set; }
    }
}
