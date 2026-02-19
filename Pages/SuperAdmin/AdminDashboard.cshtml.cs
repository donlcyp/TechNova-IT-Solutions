using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class AdminDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int TotalPolicies { get; set; }
        public int PendingCompliance { get; set; }
        public int TotalSuppliers { get; set; }
        public int RecentProcurements { get; set; }
        public int AuditLogsToday { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.SuperAdmin });
            if (denied != null) return denied;

            TotalUsers = await _context.Users.CountAsync();
            TotalPolicies = await _context.Policies.CountAsync();
            PendingCompliance = await _context.ComplianceStatuses.CountAsync(c => c.Status == "Pending");
            TotalSuppliers = await _context.Suppliers.CountAsync();
            RecentProcurements = await _context.Procurements.CountAsync(p => p.PurchaseDate >= DateTime.UtcNow.AddDays(-30));
            AuditLogsToday = await _context.AuditLogs.CountAsync(a => a.LogDate.Date == DateTime.Today);

            return Page();
        }
    }
}



