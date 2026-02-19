
namespace TechNova_IT_Solutions.Pages.Employee
{
    public class ComplianceStatusModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public ComplianceStatusModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public int ComplianceScore { get; set; } = 0;
        public int TotalPolicies { get; set; } = 0;
        public int AcknowledgedPolicies { get; set; } = 0;
        public int PendingPolicies { get; set; } = 0;
        public string LastUpdated { get; set; } = string.Empty;
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
            if (userRole != RoleNames.Employee && userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.ComplianceManager)
                {
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            int userId = int.Parse(userIdString);

            // Get compliance status data from service
            var statusData = await _employeeService.GetEmployeeComplianceStatusAsync(userId);
            
            ComplianceScore = statusData.ComplianceScore;
            TotalPolicies = statusData.TotalPolicies;
            AcknowledgedPolicies = statusData.AcknowledgedPolicies;
            PendingPolicies = statusData.PendingPolicies;
            LastUpdated = statusData.LastUpdated;

            // Get policy list for detailed sections
            PolicyList = await _employeeService.GetAssignedPoliciesAsync(userId);

            return Page();
        }
    }
}





