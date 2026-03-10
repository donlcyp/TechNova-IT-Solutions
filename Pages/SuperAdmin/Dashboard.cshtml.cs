using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class DashboardRecentLog
    {
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime LogDate { get; set; }
    }

    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // System-wide stats
        public int TotalBranches { get; set; }
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int TotalProcurements { get; set; }
        public decimal SystemComplianceRate { get; set; }

        // Extended stats
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int OpenViolations { get; set; }
        public int CriticalViolations { get; set; }
        public int PendingProcurements { get; set; }
        public int PendingPolicyReviews { get; set; }
        public int ActiveSuppliers { get; set; }
        public int TotalComplianceManagers { get; set; }

        // Recent activity
        public List<DashboardRecentLog> RecentAuditLogs { get; set; } = new();

        // Procurement breakdown
        public int ProcurementApproved { get; set; }
        public int ProcurementRejected { get; set; }
        public int ProcurementDraft { get; set; }

        public async Task<IActionResult> OnGetAsync()
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

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "superadmin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Super Administrator";

            // Core aggregates
            TotalBranches = await _context.Branches.CountAsync();
            TotalUsers = await _context.Users.CountAsync();
            TotalAdmins = await _context.Users.CountAsync(u => u.Role == RoleNames.SystemAdmin || u.Role == RoleNames.BranchAdmin);
            TotalEmployees = await _context.Users.CountAsync(u => u.Role == RoleNames.Employee);
            TotalSuppliers = await _context.Suppliers.CountAsync();
            TotalPolicies = await _context.Policies.CountAsync();
            ActivePolicies = await _context.Policies.CountAsync(p => !p.IsArchived);
            TotalProcurements = await _context.Procurements.CountAsync();

            // Extended aggregates
            ActiveUsers = await _context.Users.CountAsync(u => u.Status == "Active");
            InactiveUsers = TotalUsers - ActiveUsers;
            OpenViolations = await _context.ComplianceViolations.CountAsync(v => v.Status == "Open" || v.Status == "UnderReview" || v.Status == "Escalated");
            CriticalViolations = await _context.ComplianceViolations.CountAsync(v => v.SeverityLevel == "Critical" && v.Status != "Resolved");
            PendingProcurements = await _context.Procurements.CountAsync(p => p.Status == "Pending" || p.Status == "Draft");
            PendingPolicyReviews = await _context.ExternalPolicyImports.CountAsync(e => e.ReviewStatus == "PendingReview");
            ActiveSuppliers = await _context.Suppliers.CountAsync(s => s.Status == "Active");
            TotalComplianceManagers = await _context.Users.CountAsync(u =>
                u.Role == RoleNames.ComplianceManager || u.Role == RoleNames.ChiefComplianceManager);

            // Procurement breakdown
            ProcurementApproved = await _context.Procurements.CountAsync(p => p.Status == "Approved");
            ProcurementRejected = await _context.Procurements.CountAsync(p => p.Status == "Rejected");
            ProcurementDraft = await _context.Procurements.CountAsync(p => p.Status == "Draft");

            // System-wide compliance rate
            var totalAssignments = await _context.PolicyAssignments.CountAsync();
            var acknowledgedAssignments = await _context.ComplianceStatuses.CountAsync(cs => cs.Status == "Acknowledged");
            SystemComplianceRate = totalAssignments == 0
                ? 0
                : Math.Round((decimal)acknowledgedAssignments * 100 / totalAssignments, 1);

            // Recent audit logs (last 8)
            RecentAuditLogs = await _context.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.LogDate)
                .Take(8)
                .Select(l => new DashboardRecentLog
                {
                    Action = l.Action ?? "Unknown action",
                    Module = l.Module ?? "System",
                    UserName = l.User != null ? l.User.FirstName + " " + l.User.LastName : "System",
                    LogDate = l.LogDate
                })
                .ToListAsync();

            return Page();
        }
    }
}



