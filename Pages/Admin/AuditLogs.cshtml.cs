
namespace TechNova_IT_Solutions.Pages
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

        public List<AuditLogEntry> AuditLogs { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check authentication
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";

            // Fetch audit logs from database
            AuditLogs = await _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.LogDate)
                .Select(al => new AuditLogEntry
                {
                    LogId = "LOG-" + al.LogId.ToString("D3"),
                    UserName = al.User != null ? $"{al.User.FirstName} {al.User.LastName}" : "System",
                    UserEmail = al.User != null ? al.User.Email : "system@technova.com",
                    Role = al.User != null ? al.User.Role : "System",
                    ActionPerformed = al.Action ?? string.Empty,
                    FullDescription = al.Action ?? string.Empty,
                    Module = al.Module ?? string.Empty,
                    DateTime = al.LogDate,
                    IpAddress = "N/A" // Not stored in current database model
                })
                .ToListAsync();

            return Page();
        }
    }

    public class AuditLogEntry
    {
        public string LogId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ActionPerformed { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }
}





