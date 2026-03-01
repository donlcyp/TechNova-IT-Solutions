
namespace TechNova_IT_Solutions.Pages.ChiefComplianceManager
{
    public class AuditTrailModel : PageModel
    {
        private readonly IComplianceManagerService _complianceService;

        public AuditTrailModel(IComplianceManagerService complianceService)
        {
            _complianceService = complianceService;
        }

        public int TotalPolicyActions { get; set; }
        public int TotalComplianceActions { get; set; }
        public int ActivitiesToday { get; set; }
        public List<AuditLogRecord> AuditLogs { get; set; } = new();

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

            // Get audit trail data from service
            var auditData = await _complianceService.GetAuditTrailDataAsync(callerBranchId);
            
            TotalPolicyActions = auditData.TotalPolicyActions;
            TotalComplianceActions = auditData.TotalComplianceActions;
            ActivitiesToday = auditData.ActivitiesToday;
            AuditLogs = auditData.AuditLogs;

            return Page();
        }
    }
}





