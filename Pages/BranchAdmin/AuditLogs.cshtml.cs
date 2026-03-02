namespace TechNova_IT_Solutions.Pages.BranchAdmin
{
    public class AuditLogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public List<TechNova_IT_Solutions.Pages.AuditLogEntry> AuditLogs { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            // Audit logs are company-level (SystemAdmin) — BranchAdmin does not have this
            if (userRole == RoleNames.BranchAdmin)
                return RedirectToPage("/BranchAdmin/Dashboard");
            if (userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.SystemAdmin) return RedirectToPage("/SystemAdmin/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";

            int? callerBranchId = null;
            if (userRole == RoleNames.BranchAdmin)
            {
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var bid))
                    callerBranchId = bid;
            }

            bool scoped = callerBranchId.HasValue;

            AuditLogs = await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => !scoped || (al.User != null && al.User.BranchId == callerBranchId))
                .OrderByDescending(al => al.LogDate)
                .Select(al => new TechNova_IT_Solutions.Pages.AuditLogEntry
                {
                    LogId = "LOG-" + al.LogId.ToString("D3"),
                    UserName = al.User != null ? $"{al.User.FirstName} {al.User.LastName}" : "System",
                    UserEmail = al.User != null ? al.User.Email : "system@technova.com",
                    Role = al.User != null ? al.User.Role : "System",
                    ActionPerformed = al.Action ?? string.Empty,
                    FullDescription = al.Action ?? string.Empty,
                    Module = al.Module ?? string.Empty,
                    DateTime = al.LogDate,
                    IpAddress = "N/A"
                })
                .ToListAsync();

            return Page();
        }
    }
}
