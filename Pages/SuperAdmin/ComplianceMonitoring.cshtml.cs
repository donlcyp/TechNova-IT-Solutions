using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class ComplianceMonitoringModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ComplianceMonitoringModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalAssignments { get; set; }
        public int CompliantEmployees { get; set; }
        public int NonCompliantEmployees { get; set; }
        public int CompliantSuppliers { get; set; }
        public List<EmployeeComplianceRow> NonCompliantEmployeeRows { get; set; } = new();
        public List<SupplierComplianceRow> SupplierExceptionRows { get; set; } = new();
        public List<AssignmentMonitoringRow> RecentAssignmentRows { get; set; } = new();

        public class EmployeeComplianceRow
        {
            public string EmployeeName { get; set; } = string.Empty;
            public string PolicyTitle { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime? AssignedDate { get; set; }
            public DateTime? AcknowledgedDate { get; set; }
        }

        public class SupplierComplianceRow
        {
            public string SupplierName { get; set; } = string.Empty;
            public string PolicyTitle { get; set; } = string.Empty;
            public string ComplianceStatus { get; set; } = string.Empty;
            public DateTime? AssignedDate { get; set; }
        }

        public class AssignmentMonitoringRow
        {
            public string AssigneeType { get; set; } = string.Empty;
            public string AssigneeName { get; set; } = string.Empty;
            public string PolicyTitle { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime? AssignedDate { get; set; }
        }

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.SuperAdmin });
            if (denied != null) return denied;

            TotalAssignments = await _context.PolicyAssignments.CountAsync();
            CompliantEmployees = await _context.ComplianceStatuses.CountAsync(c => c.Status == "Acknowledged");
            NonCompliantEmployees = await _context.ComplianceStatuses.CountAsync(c => c.Status == "Pending" || c.Status == "Overdue");
            CompliantSuppliers = await _context.SupplierPolicies
                .Where(s => s.ComplianceStatus == "Compliant")
                .Select(s => s.SupplierId)
                .Distinct()
                .CountAsync();

            NonCompliantEmployeeRows = await _context.ComplianceStatuses
                .AsNoTracking()
                .Where(c => c.Status == "Pending" || c.Status == "Overdue")
                .OrderByDescending(c => c.PolicyAssignment.AssignedDate)
                .ThenBy(c => c.Status)
                .Select(c => new EmployeeComplianceRow
                {
                    EmployeeName = c.PolicyAssignment.User.FirstName + " " + c.PolicyAssignment.User.LastName,
                    PolicyTitle = c.PolicyAssignment.Policy.PolicyTitle,
                    Status = c.Status,
                    AssignedDate = c.PolicyAssignment.AssignedDate,
                    AcknowledgedDate = c.AcknowledgedDate
                })
                .Take(10)
                .ToListAsync();

            SupplierExceptionRows = await _context.SupplierPolicies
                .AsNoTracking()
                .Where(s => s.ComplianceStatus == "Pending" || s.ComplianceStatus == "Non-Compliant")
                .OrderByDescending(s => s.AssignedDate)
                .ThenBy(s => s.ComplianceStatus)
                .Select(s => new SupplierComplianceRow
                {
                    SupplierName = s.Supplier.SupplierName,
                    PolicyTitle = s.Policy.PolicyTitle,
                    ComplianceStatus = s.ComplianceStatus,
                    AssignedDate = s.AssignedDate
                })
                .Take(10)
                .ToListAsync();

            var recentEmployeeAssignments = await _context.PolicyAssignments
                .AsNoTracking()
                .OrderByDescending(pa => pa.AssignedDate)
                .Select(pa => new AssignmentMonitoringRow
                {
                    AssigneeType = "Employee",
                    AssigneeName = pa.User.FirstName + " " + pa.User.LastName,
                    PolicyTitle = pa.Policy.PolicyTitle,
                    Status = pa.ComplianceStatus != null ? pa.ComplianceStatus.Status : "Pending",
                    AssignedDate = pa.AssignedDate
                })
                .Take(8)
                .ToListAsync();

            var recentSupplierAssignments = await _context.SupplierPolicies
                .AsNoTracking()
                .OrderByDescending(sp => sp.AssignedDate)
                .Select(sp => new AssignmentMonitoringRow
                {
                    AssigneeType = "Supplier",
                    AssigneeName = sp.Supplier.SupplierName,
                    PolicyTitle = sp.Policy.PolicyTitle,
                    Status = sp.ComplianceStatus,
                    AssignedDate = sp.AssignedDate
                })
                .Take(8)
                .ToListAsync();

            RecentAssignmentRows = recentEmployeeAssignments
                .Concat(recentSupplierAssignments)
                .OrderByDescending(a => a.AssignedDate)
                .Take(10)
                .ToList();

            return Page();
        }
    }
}



