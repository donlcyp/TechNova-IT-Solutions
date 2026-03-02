
namespace TechNova_IT_Solutions.Pages.Employee
{
    public class DashboardModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public DashboardModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public string EmployeeName { get; set; } = RoleNames.Employee;
        public int AssignedPolicies { get; set; } = 0;
        public int PendingPolicies { get; set; } = 0;
        public int ComplianceRate { get; set; } = 0;
        public int AcknowledgedPolicies { get; set; } = 0;
        public List<AssignedPolicyData> PolicyList { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Employee and Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.Employee && !RoleNames.IsAdminRole(userRole) && userRole != RoleNames.SuperAdmin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager)
                {
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Get user name from session
            EmployeeName = HttpContext.Session.GetString(SessionKeys.UserName) ?? RoleNames.Employee;
            
            int userId = int.Parse(userIdString);

            // Get dashboard data from service
            var dashboardData = await _employeeService.GetEmployeeDashboardDataAsync(userId);
            
            AssignedPolicies = dashboardData.AssignedPolicies;
            PendingPolicies = dashboardData.PendingPolicies;
            ComplianceRate = dashboardData.ComplianceRate;
            AcknowledgedPolicies = dashboardData.AcknowledgedPolicies;

            // Get assigned policies list
            PolicyList = await _employeeService.GetAssignedPoliciesAsync(userId);

            return Page();
        }
    }
}





