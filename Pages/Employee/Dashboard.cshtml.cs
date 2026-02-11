using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.Employee
{
    public class DashboardModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public DashboardModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public string EmployeeName { get; set; } = "Employee";
        public int AssignedPolicies { get; set; } = 0;
        public int PendingPolicies { get; set; } = 0;
        public int ComplianceRate { get; set; } = 0;
        public int AcknowledgedPolicies { get; set; } = 0;
        public List<AssignedPolicyData> PolicyList { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Employee and Admin can access
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Employee" && userRole != "Admin")
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == "ComplianceManager")
                {
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Get user name from session
            EmployeeName = HttpContext.Session.GetString("UserName") ?? "Employee";
            
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
