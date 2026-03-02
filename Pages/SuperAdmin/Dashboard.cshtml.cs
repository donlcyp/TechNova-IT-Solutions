using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
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

            // System-wide aggregates
            TotalBranches = await _context.Branches.CountAsync();
            TotalUsers = await _context.Users.CountAsync();
            TotalAdmins = await _context.Users.CountAsync(u => u.Role == RoleNames.SystemAdmin || u.Role == RoleNames.BranchAdmin);
            TotalEmployees = await _context.Users.CountAsync(u => u.Role == RoleNames.Employee);
            TotalSuppliers = await _context.Suppliers.CountAsync();
            TotalPolicies = await _context.Policies.CountAsync();
            ActivePolicies = await _context.Policies.CountAsync(p => !p.IsArchived);
            TotalProcurements = await _context.Procurements.CountAsync();

            // System-wide compliance rate (acknowledged / total assignments)
            var totalAssignments = await _context.PolicyAssignments.CountAsync();
            var acknowledgedAssignments = await _context.ComplianceStatuses
                .CountAsync(cs => cs.Status == "Acknowledged");
            SystemComplianceRate = totalAssignments == 0
                ? 0
                : Math.Round((decimal)acknowledgedAssignments * 100 / totalAssignments, 1);

            return Page();
        }
    }
}



