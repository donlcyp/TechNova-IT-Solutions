using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class DetailedReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailedReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string ModuleType { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        public List<DetailedDrilldownRow> Rows { get; set; } = new();

        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CompliantCount { get; set; }
        public int PendingCount { get; set; }
        public int NonCompliantCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.SuperAdmin });
            if (denied != null) return denied;

            NormalizeFilters();
            var filteredRows = await BuildFilteredRowsAsync();
            PopulateSummary(filteredRows);
            ApplyPagination(filteredRows);
            return Page();
        }

        public async Task<IActionResult> OnGetExportCsvAsync(string moduleType = "all", string status = "all", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.SuperAdmin });
            if (denied != null) return denied;

            ModuleType = moduleType;
            Status = status;
            FromDate = fromDate;
            ToDate = toDate;

            NormalizeFilters();
            var filteredRows = await BuildFilteredRowsAsync();

            var csv = BuildCsv(filteredRows);
            var fileName = $"superadmin-detailed-reports-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        private void NormalizeFilters()
        {
            ModuleType = (ModuleType ?? "all").Trim().ToLowerInvariant();
            if (ModuleType != "all" && ModuleType != "employee" && ModuleType != "supplier")
            {
                ModuleType = "all";
            }

            Status = (Status ?? "all").Trim().ToLowerInvariant();
            if (Status != "all" && Status != "compliant" && Status != "pending" && Status != "non-compliant")
            {
                Status = "all";
            }

            if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            {
                (FromDate, ToDate) = (ToDate, FromDate);
            }

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize is not (10 or 25 or 50)) PageSize = 25;
        }

        private async Task<List<DetailedDrilldownRow>> BuildFilteredRowsAsync()
        {
            var startDate = FromDate?.Date;
            var endDateExclusive = ToDate?.Date.AddDays(1);

            var rows = new List<DetailedDrilldownRow>();

            if (ModuleType == "all" || ModuleType == "employee")
            {
                var employeeQuery = _context.PolicyAssignments
                    .AsNoTracking()
                    .Include(pa => pa.User)
                    .Include(pa => pa.Policy)
                    .Include(pa => pa.ComplianceStatus)
                    .Where(pa => pa.User != null && pa.User.Role == RoleNames.Employee);

                if (startDate.HasValue)
                {
                    employeeQuery = employeeQuery.Where(pa => pa.AssignedDate.HasValue && pa.AssignedDate.Value >= startDate.Value);
                }
                if (endDateExclusive.HasValue)
                {
                    employeeQuery = employeeQuery.Where(pa => pa.AssignedDate.HasValue && pa.AssignedDate.Value < endDateExclusive.Value);
                }

                var employeeRows = await employeeQuery
                    .Select(pa => new DetailedDrilldownRow
                    {
                        Module = "Employee",
                        EntityName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                        Policy = pa.Policy != null ? (pa.Policy.PolicyTitle ?? "Unknown") : "Unknown",
                        Status = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged"
                            ? "Compliant"
                            : pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Overdue"
                                ? "Non-Compliant"
                                : "Pending",
                        AssignedDate = pa.AssignedDate
                    })
                    .ToListAsync();

                rows.AddRange(employeeRows);
            }

            if (ModuleType == "all" || ModuleType == "supplier")
            {
                var supplierQuery = _context.SupplierPolicies
                    .AsNoTracking()
                    .Include(sp => sp.Supplier)
                    .Include(sp => sp.Policy)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    supplierQuery = supplierQuery.Where(sp => sp.AssignedDate.HasValue && sp.AssignedDate.Value >= startDate.Value);
                }
                if (endDateExclusive.HasValue)
                {
                    supplierQuery = supplierQuery.Where(sp => sp.AssignedDate.HasValue && sp.AssignedDate.Value < endDateExclusive.Value);
                }

                var supplierRows = await supplierQuery
                    .Select(sp => new DetailedDrilldownRow
                    {
                        Module = "Supplier",
                        EntityName = sp.Supplier != null ? (sp.Supplier.SupplierName ?? "Unknown") : "Unknown",
                        Policy = sp.Policy != null ? (sp.Policy.PolicyTitle ?? "Unknown") : "Unknown",
                        Status = string.IsNullOrWhiteSpace(sp.ComplianceStatus) ? "Pending" : sp.ComplianceStatus,
                        AssignedDate = sp.AssignedDate
                    })
                    .ToListAsync();

                rows.AddRange(supplierRows);
            }

            if (Status != "all")
            {
                rows = rows
                    .Where(r => r.Status.Equals(Status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return rows
                .OrderByDescending(r => r.AssignedDate)
                .ThenBy(r => r.Module)
                .ThenBy(r => r.EntityName)
                .ToList();
        }

        private void PopulateSummary(List<DetailedDrilldownRow> filteredRows)
        {
            TotalRecords = filteredRows.Count;
            CompliantCount = filteredRows.Count(r => r.Status.Equals("Compliant", StringComparison.OrdinalIgnoreCase));
            PendingCount = filteredRows.Count(r => r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
            NonCompliantCount = filteredRows.Count(r => r.Status.Equals("Non-Compliant", StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyPagination(List<DetailedDrilldownRow> filteredRows)
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            Rows = filteredRows
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private static string BuildCsv(List<DetailedDrilldownRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Module,Entity,Policy,Status,Assigned Date");
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(row.Module),
                    EscapeCsv(row.EntityName),
                    EscapeCsv(row.Policy),
                    EscapeCsv(row.Status),
                    EscapeCsv(row.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A")));
            }
            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }

    public class DetailedDrilldownRow
    {
        public string Module { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string Policy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? AssignedDate { get; set; }
    }
}
