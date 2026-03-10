using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.SystemAdmin
{
    public class BranchManagementModel : PageModel
    {
        private readonly IBranchService _branchService;

        public BranchManagementModel(IBranchService branchService)
        {
            _branchService = branchService;
        }

        public List<BranchData> Branches { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.SystemAdmin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.BranchAdmin)            return RedirectToPage("/BranchAdmin/Dashboard");
                if (userRole == RoleNames.Employee)               return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager)
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            Branches = await _branchService.GetAllBranchesAsync();
            return Page();
        }
    }
}
