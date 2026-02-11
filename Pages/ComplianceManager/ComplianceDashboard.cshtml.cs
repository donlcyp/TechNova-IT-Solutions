using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class ComplianceDashboardModel : PageModel
    {
        private readonly IComplianceManagerService _complianceService;

        public ComplianceDashboardModel(IComplianceManagerService complianceService)
        {
            _complianceService = complianceService;
        }

        public int TotalPoliciesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int SuppliersCompliant { get; set; }

        public List<EmployeeComplianceData> EmployeeCompliance { get; set; } = new();
        public List<SupplierComplianceData> SupplierCompliance { get; set; } = new();
        public List<RecentlyAssignedData> RecentlyAssigned { get; set; } = new();

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

            // Get dashboard data from service
            var dashboardData = await _complianceService.GetComplianceDashboardDataAsync();
            
            TotalPoliciesAssigned = dashboardData.TotalPoliciesAssigned;
            EmployeesCompliant = dashboardData.EmployeesCompliant;
            EmployeesNotCompliant = dashboardData.EmployeesNotCompliant;
            SuppliersCompliant = dashboardData.SuppliersCompliant;
            EmployeeCompliance = dashboardData.EmployeeCompliance;
            SupplierCompliance = dashboardData.SupplierCompliance;
            RecentlyAssigned = dashboardData.RecentlyAssigned;

            return Page();
        }
    }
}
