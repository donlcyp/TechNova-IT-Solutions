using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class ComplianceReportsModel : PageModel
    {
        private readonly IComplianceManagerService _complianceService;

        public ComplianceReportsModel(IComplianceManagerService complianceService)
        {
            _complianceService = complianceService;
        }

        public int TotalPolicies { get; set; }
        public int TotalEmployees { get; set; }
        public int CompliantEmployees { get; set; }
        public int NonCompliantEmployees { get; set; }
        public double ComplianceRate { get; set; }
        public List<PolicyComplianceData> PolicyCompliance { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only ComplianceManager and Admin can access
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ComplianceManager" && userRole != "Admin")
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == "Employee")
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Get compliance reports data from service
            var reportsData = await _complianceService.GetComplianceReportsDataAsync();
            
            TotalPolicies = reportsData.TotalPolicies;
            TotalEmployees = reportsData.TotalEmployees;
            CompliantEmployees = reportsData.CompliantEmployees;
            NonCompliantEmployees = reportsData.NonCompliantEmployees;
            ComplianceRate = reportsData.ComplianceRate;
            PolicyCompliance = reportsData.PolicyCompliance;

            return Page();
        }
    }
}
