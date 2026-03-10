using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.SystemAdmin
{
    public class MainBranchUsersModel : PageModel
    {
        private readonly IUserService _userService;

        public MainBranchUsersModel(IUserService userService)
        {
            _userService = userService;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public List<UserData> Users { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.SystemAdmin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.BranchAdmin) return RedirectToPage("/BranchAdmin/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager)
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";
            IsSuperAdmin = userRole == RoleNames.SuperAdmin;

            var allUsers = await _userService.GetAllUsersAsync();

            // Main Branch users: Employee or ComplianceManager with no branch assigned (BranchId == null)
            Users = allUsers
                .Where(u => u.BranchId == null &&
                            (u.Role == RoleNames.Employee || u.Role == RoleNames.ComplianceManager))
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FirstName)
                .ToList();

            return Page();
        }
    }
}
