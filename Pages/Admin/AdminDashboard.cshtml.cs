
namespace TechNova_IT_Solutions.Pages
{
    public class AdminDashboardModel : PageModel
    {
        private readonly IAdminService _adminService;

        public AdminDashboardModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // Top Summary Cards Data
        public int TotalUsers { get; set; } = 0;
        public int ActivePolicies { get; set; } = 0;
        public int PendingCompliance { get; set; } = 0;
        public int TotalSuppliers { get; set; } = 0;
        public int RecentProcurements { get; set; } = 0;
        public int AuditLogsToday { get; set; } = 0;

        // Compliance Overview
        public int CompliancePercentage { get; set; } = 0;
        public List<PolicyItem> RecentPolicies { get; set; } = new();

        // Procurement Overview
        public List<ProcurementItem> RecentProcurementsData { get; set; } = new();

        // Recent Activities
        public List<ActivityItem> RecentActivities { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
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

            // Get dashboard data from service
            var dashboardData = await _adminService.GetDashboardDataAsync();
            
            // Map data to PageModel properties
            TotalUsers = dashboardData.TotalUsers;
            ActivePolicies = dashboardData.ActivePolicies;
            PendingCompliance = dashboardData.PendingCompliance;
            TotalSuppliers = dashboardData.TotalSuppliers;
            RecentProcurements = dashboardData.RecentProcurements;
            AuditLogsToday = dashboardData.AuditLogsToday;
            CompliancePercentage = dashboardData.CompliancePercentage;
            RecentPolicies = dashboardData.RecentPolicies;
            RecentProcurementsData = dashboardData.RecentProcurementsData;
            RecentActivities = dashboardData.RecentActivities;

            return Page();
        }
    }
}





