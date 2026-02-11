using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.Employee
{
    public class AssignedPoliciesModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public AssignedPoliciesModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public List<AssignedPolicyData> Policies { get; set; } = new();
        public int TotalAssigned { get; set; }
        public int PendingCount { get; set; }
        public int AcknowledgedCount { get; set; }

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

            int userId = int.Parse(userIdString);

            // Load assigned policies from database
            Policies = await _employeeService.GetAssignedPoliciesAsync(userId);
            TotalAssigned = Policies.Count;
            PendingCount = Policies.Count(p => p.Status == "Pending");
            AcknowledgedCount = Policies.Count(p => p.Status == "Acknowledged");

            return Page();
        }
    }
}
