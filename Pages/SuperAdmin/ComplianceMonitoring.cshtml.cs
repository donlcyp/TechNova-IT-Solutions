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

            return Page();
        }
    }
}



