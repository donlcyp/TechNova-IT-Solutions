
namespace TechNova_IT_Solutions.Pages.ComplianceManager
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

            // Check user role - only ComplianceManager and Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ComplianceManager && userRole != RoleNames.Admin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Extract branch scope
            int? callerBranchId = null;
            if (userRole == RoleNames.ComplianceManager || userRole == RoleNames.Admin)
            {
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var bid))
                    callerBranchId = bid;
            }

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





