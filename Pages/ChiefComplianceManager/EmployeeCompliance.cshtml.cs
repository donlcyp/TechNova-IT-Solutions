
namespace TechNova_IT_Solutions.Pages.ChiefComplianceManager
{
    public class EmployeeComplianceModel : PageModel
    {
        private readonly IComplianceManagerService _complianceService;

        public EmployeeComplianceModel(IComplianceManagerService complianceService)
        {
            _complianceService = complianceService;
        }

        public int TotalEmployeesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int RecentlyAcknowledged { get; set; }

        public List<EmployeeRecord> EmployeeRecords { get; set; } = new();
        public List<EmployeeDetail> EmployeeDetails { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only ChiefComplianceManager and SuperAdmin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ChiefComplianceManager && userRole != RoleNames.SuperAdmin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Chief CM always sees all branches — no branch scoping
            int? callerBranchId = null;

            // Get employee compliance data from service
            var reportData = await _complianceService.GetEmployeeComplianceReportAsync(callerBranchId);
            
            TotalEmployeesAssigned = reportData.TotalEmployeesAssigned;
            EmployeesCompliant = reportData.EmployeesCompliant;
            EmployeesNotCompliant = reportData.EmployeesNotCompliant;
            RecentlyAcknowledged = reportData.RecentlyAcknowledged;
            EmployeeRecords = reportData.EmployeeRecords;
            EmployeeDetails = reportData.EmployeeDetails;

            return Page();
        }
    }
}





