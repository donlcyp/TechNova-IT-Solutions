using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages
{
    public class UserManagementModel : PageModel
    {
        private readonly IUserService _userService;

        public UserManagementModel(IUserService userService)
        {
            _userService = userService;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public List<UserData> Users { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check authentication
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Admin can access
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                if (userRole == "Employee") return RedirectToPage("/Employee/Dashboard");
                if (userRole == "ComplianceManager") return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString("UserEmail") ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            // Fetch users from service
            Users = await _userService.GetAllUsersAsync();

            return Page();
        }
    }
}
