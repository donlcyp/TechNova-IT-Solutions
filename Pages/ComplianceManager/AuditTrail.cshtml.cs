using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.ComplianceManager
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

            // Get audit trail data from service
            var auditData = await _complianceService.GetAuditTrailDataAsync();
            
            TotalPolicyActions = auditData.TotalPolicyActions;
            TotalComplianceActions = auditData.TotalComplianceActions;
            ActivitiesToday = auditData.ActivitiesToday;
            AuditLogs = auditData.AuditLogs;

            return Page();
        }
    }
}
