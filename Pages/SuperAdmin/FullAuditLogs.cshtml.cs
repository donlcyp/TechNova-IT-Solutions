using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class FullAuditLogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FullAuditLogsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedBranchId { get; set; }

        public List<AuditLogItem> Logs { get; set; } = new();
        public List<BranchFilterItem> Branches { get; set; } = new();

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

            Branches = await _context.Branches
                .OrderBy(b => b.BranchName)
                .Select(b => new BranchFilterItem
                {
                    BranchId = b.BranchId,
                    BranchName = b.BranchName
                })
                .ToListAsync();

            Logs = await _context.AuditLogs
                .Include(a => a.User)
                    .ThenInclude(u => u!.Branch)
                .Where(a => !SelectedBranchId.HasValue ||
                            (a.User != null && a.User.BranchId == SelectedBranchId))
                .OrderByDescending(a => a.LogDate)
                .Take(300)
                .Select(a => new AuditLogItem
                {
                    LogId = a.LogId,
                    UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "System",
                    Role = a.User != null ? a.User.Role : "System",
                    BranchName = a.User != null && a.User.Branch != null ? a.User.Branch.BranchName : "—",
                    Action = a.Action ?? string.Empty,
                    Module = a.Module ?? string.Empty,
                    DateTime = a.LogDate
                })
                .ToListAsync();

            return Page();
        }
    }

    public class AuditLogItem
    {
        public int LogId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
    }

    public class BranchFilterItem
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }
}



