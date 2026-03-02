namespace TechNova_IT_Solutions.Pages.BranchAdmin
{
    public class DashboardModel : PageModel
    {
        private readonly IAdminService _adminService;

        public DashboardModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public int TotalUsers { get; set; } = 0;
        public int ActivePolicies { get; set; } = 0;
        public int PendingCompliance { get; set; } = 0;
        public int TotalSuppliers { get; set; } = 0;
        public int RecentProcurements { get; set; } = 0;
        public int AuditLogsToday { get; set; } = 0;

        public int CompliancePercentage { get; set; } = 0;
        public List<PolicyItem> RecentPolicies { get; set; } = new();

        public List<ProcurementItem> RecentProcurementsData { get; set; } = new();

        public List<ActivityItem> RecentActivities { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.BranchAdmin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.SystemAdmin) return RedirectToPage("/SystemAdmin/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";

            int? callerBranchId = null;
            var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
            if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var parsedBranchId))
                callerBranchId = parsedBranchId;

            var dashboardData = await _adminService.GetDashboardDataAsync(callerBranchId);

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
