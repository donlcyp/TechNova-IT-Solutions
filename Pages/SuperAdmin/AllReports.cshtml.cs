using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class AllReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AllReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int PeriodDays { get; set; } = 30;

        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

        public int TotalUsers { get; set; }
        public int TotalActiveUsers { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalProcurementTransactions { get; set; }
        public decimal SystemComplianceRate { get; set; }

        public decimal EmployeeComplianceRate { get; set; }
        public decimal SupplierComplianceRate { get; set; }
        public int OverduePolicies { get; set; }
        public decimal CurrentMonthComplianceRate { get; set; }
        public decimal PreviousMonthComplianceRate { get; set; }

        public int TotalLoginAttempts { get; set; }
        public int FailedLoginAttempts { get; set; }
        public int LockedAccounts { get; set; }
        public string MostActiveUser { get; set; } = "N/A";
        public string MostAccessedModule { get; set; } = "N/A";
        public int SystemErrorsLogged { get; set; }

        public int ApprovedOrders { get; set; }
        public int RejectedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalProcurementQuantity { get; set; }
        public int DelayedShipments { get; set; }
        public List<SupplierVolumeItem> TopSuppliersByVolume { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(
                this,
                new[] { RoleNames.SuperAdmin },
                new Dictionary<string, string>
                {
                    [RoleNames.ChiefComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.ComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.Employee] = "/Employee/Dashboard",
                    [RoleNames.Supplier] = "/Supplier/Dashboard"
                });
            if (denied != null) return denied;

            if (PeriodDays is not (7 or 30 or 90))
            {
                PeriodDays = 30;
            }

            GeneratedBy = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Super Admin";

            await LoadSystemOverviewAsync();
            await LoadComplianceOverviewAsync();
            await LoadSecurityOverviewAsync();
            await LoadProcurementOverviewAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetExportSystemCsvAsync(int periodDays = 30)
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.SuperAdmin });
            if (denied != null) return denied;

            PeriodDays = periodDays is (7 or 30 or 90) ? periodDays : 30;
            GeneratedBy = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Super Admin";

            await LoadSystemOverviewAsync();
            await LoadComplianceOverviewAsync();
            await LoadSecurityOverviewAsync();
            await LoadProcurementOverviewAsync();

            var csv = BuildCsv();
            var fileName = $"superadmin-system-overview-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        private async Task LoadSystemOverviewAsync()
        {
            TotalUsers = await _context.Users.CountAsync();
            TotalActiveUsers = await _context.Users.CountAsync(u => u.Status == "Active");
            TotalPolicies = await _context.Policies.CountAsync();
            TotalSuppliers = await _context.Suppliers.CountAsync();
            TotalProcurementTransactions = await _context.Procurements.CountAsync();

            var totalEmployeeAssignments = await _context.PolicyAssignments.CountAsync();
            var employeeCompliantAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .CountAsync(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged");

            var totalSupplierAssignments = await _context.SupplierPolicies.CountAsync();
            var supplierCompliantAssignments = await _context.SupplierPolicies
                .CountAsync(sp => sp.ComplianceStatus == "Compliant");

            var totalAssignments = totalEmployeeAssignments + totalSupplierAssignments;
            var totalCompliant = employeeCompliantAssignments + supplierCompliantAssignments;
            SystemComplianceRate = totalAssignments == 0
                ? 0
                : Math.Round((decimal)totalCompliant * 100 / totalAssignments, 1);
        }

        private async Task LoadComplianceOverviewAsync()
        {
            var totalEmployeeAssignments = await _context.PolicyAssignments.CountAsync();
            var employeeCompliantAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .CountAsync(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged");
            EmployeeComplianceRate = totalEmployeeAssignments == 0
                ? 0
                : Math.Round((decimal)employeeCompliantAssignments * 100 / totalEmployeeAssignments, 1);

            var totalSupplierAssignments = await _context.SupplierPolicies.CountAsync();
            var supplierCompliantAssignments = await _context.SupplierPolicies
                .CountAsync(sp => sp.ComplianceStatus == "Compliant");
            SupplierComplianceRate = totalSupplierAssignments == 0
                ? 0
                : Math.Round((decimal)supplierCompliantAssignments * 100 / totalSupplierAssignments, 1);

            OverduePolicies = await _context.ComplianceStatuses.CountAsync(cs => cs.Status == "Overdue");

            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            CurrentMonthComplianceRate = await CalculateMonthlyComplianceRate(currentMonthStart, now);
            PreviousMonthComplianceRate = await CalculateMonthlyComplianceRate(previousMonthStart, currentMonthStart);
        }

        private async Task<decimal> CalculateMonthlyComplianceRate(DateTime fromInclusive, DateTime toExclusive)
        {
            var employeeAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.AssignedDate.HasValue
                    && pa.AssignedDate.Value >= fromInclusive
                    && pa.AssignedDate.Value < toExclusive)
                .ToListAsync();

            var supplierAssignments = await _context.SupplierPolicies
                .Where(sp => sp.AssignedDate.HasValue
                    && sp.AssignedDate.Value >= fromInclusive
                    && sp.AssignedDate.Value < toExclusive)
                .ToListAsync();

            var total = employeeAssignments.Count + supplierAssignments.Count;
            if (total == 0)
            {
                return 0;
            }

            var compliant = employeeAssignments.Count(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged")
                + supplierAssignments.Count(sp => sp.ComplianceStatus == "Compliant");

            return Math.Round((decimal)compliant * 100 / total, 1);
        }

        private async Task LoadSecurityOverviewAsync()
        {
            var periodStart = DateTime.UtcNow.AddDays(-PeriodDays);
            var periodAuditLogs = _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.LogDate >= periodStart);

            TotalLoginAttempts = await periodAuditLogs.CountAsync(a => a.Action != null && a.Action.ToLower().Contains("login"));
            FailedLoginAttempts = await periodAuditLogs.CountAsync(a =>
                a.Action != null &&
                (a.Action.ToLower().Contains("failed") || a.Action.ToLower().Contains("invalid")));
            LockedAccounts = await _context.Users.CountAsync(u => u.Status == "Inactive");
            SystemErrorsLogged = await periodAuditLogs.CountAsync(a =>
                a.Action != null &&
                (a.Action.ToLower().Contains("error") || a.Action.ToLower().Contains("exception")));

            MostActiveUser = await periodAuditLogs
                .Include(a => a.User)
                .Where(a => a.User != null)
                .GroupBy(a => new { a.User!.FirstName, a.User.LastName })
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key.FirstName} {g.Key.LastName}")
                .FirstOrDefaultAsync() ?? "N/A";

            MostAccessedModule = await periodAuditLogs
                .Where(a => a.Module != null && a.Module != "")
                .GroupBy(a => a.Module)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key!)
                .FirstOrDefaultAsync() ?? "N/A";
        }

        private async Task LoadProcurementOverviewAsync()
        {
            var periodStart = DateTime.UtcNow.AddDays(-PeriodDays);
            var procurements = _context.Procurements
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Where(p => p.PurchaseDate.HasValue && p.PurchaseDate.Value >= periodStart);

            ApprovedOrders = await procurements.CountAsync(p => p.Status.Contains("Approved"));
            RejectedOrders = await procurements.CountAsync(p => p.Status.Contains("Reject"));
            PendingOrders = await procurements.CountAsync(p => !p.Status.Contains("Approved") && !p.Status.Contains("Reject"));
            TotalProcurementQuantity = await procurements.SumAsync(p => p.Quantity ?? 0);

            DelayedShipments = await procurements.CountAsync(p =>
                p.SupplierCommitShipDate.HasValue &&
                ((p.ReceivedDate.HasValue && p.ReceivedDate.Value > p.SupplierCommitShipDate.Value) ||
                 (!p.ReceivedDate.HasValue && p.SupplierCommitShipDate.Value < DateTime.UtcNow)));

            TopSuppliersByVolume = await procurements
                .GroupBy(p => p.Supplier != null ? p.Supplier.SupplierName : "Unknown")
                .Select(g => new SupplierVolumeItem
                {
                    SupplierName = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity ?? 0),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(g => g.TotalQuantity)
                .ThenByDescending(g => g.TransactionCount)
                .Take(3)
                .ToListAsync();
        }

        private string BuildCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Section,Metric,Value");

            sb.AppendLine($"System Overview,Total Users,{TotalUsers}");
            sb.AppendLine($"System Overview,Active Users,{TotalActiveUsers}");
            sb.AppendLine($"System Overview,Total Policies,{TotalPolicies}");
            sb.AppendLine($"System Overview,Total Suppliers,{TotalSuppliers}");
            sb.AppendLine($"System Overview,Total Procurement Transactions,{TotalProcurementTransactions}");
            sb.AppendLine($"System Overview,System Compliance Rate,{SystemComplianceRate}%");

            sb.AppendLine($"Compliance Overview,Employee Compliance Rate,{EmployeeComplianceRate}%");
            sb.AppendLine($"Compliance Overview,Supplier Compliance Rate,{SupplierComplianceRate}%");
            sb.AppendLine($"Compliance Overview,Overdue Policies,{OverduePolicies}");
            sb.AppendLine($"Compliance Overview,Current Month Compliance Rate,{CurrentMonthComplianceRate}%");
            sb.AppendLine($"Compliance Overview,Previous Month Compliance Rate,{PreviousMonthComplianceRate}%");

            sb.AppendLine($"Security and Audit,Total Login Attempts ({PeriodDays}d),{TotalLoginAttempts}");
            sb.AppendLine($"Security and Audit,Failed Login Attempts ({PeriodDays}d),{FailedLoginAttempts}");
            sb.AppendLine($"Security and Audit,Locked Accounts,{LockedAccounts}");
            sb.AppendLine($"Security and Audit,Most Active User,{EscapeCsv(MostActiveUser)}");
            sb.AppendLine($"Security and Audit,Most Accessed Module,{EscapeCsv(MostAccessedModule)}");
            sb.AppendLine($"Security and Audit,System Errors Logged ({PeriodDays}d),{SystemErrorsLogged}");

            sb.AppendLine($"Procurement Overview,Approved Orders ({PeriodDays}d),{ApprovedOrders}");
            sb.AppendLine($"Procurement Overview,Rejected Orders ({PeriodDays}d),{RejectedOrders}");
            sb.AppendLine($"Procurement Overview,Pending Orders ({PeriodDays}d),{PendingOrders}");
            sb.AppendLine($"Procurement Overview,Total Quantity ({PeriodDays}d),{TotalProcurementQuantity}");
            sb.AppendLine($"Procurement Overview,Delayed Shipments ({PeriodDays}d),{DelayedShipments}");

            foreach (var supplier in TopSuppliersByVolume)
            {
                sb.AppendLine($"Top Suppliers,Top Supplier,{EscapeCsv(supplier.SupplierName)} ({supplier.TotalQuantity} units / {supplier.TransactionCount} tx)");
            }

            return sb.ToString();
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

    public class SupplierVolumeItem
    {
        public string SupplierName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public int TransactionCount { get; set; }
    }
}



