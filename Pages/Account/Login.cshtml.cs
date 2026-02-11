using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAuthenticationService _authService;

        public LoginModel(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Clear any previous error messages
            ErrorMessage = null;
        }

        public async Task<IActionResult> OnPost()
        {
            // Authenticate using the service
            var result = await _authService.AuthenticateUserAsync(Email, Password);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return Page();
            }

            // Store user info in Session
            HttpContext.Session.SetString("UserId", result.User!.UserId.ToString());
            HttpContext.Session.SetString("UserEmail", result.User.Email);
            HttpContext.Session.SetString("UserName", $"{result.User.FirstName} {result.User.LastName}");
            HttpContext.Session.SetString("UserRole", result.User.Role);
            
            if (RememberMe)
            {
                HttpContext.Session.SetString("RememberMe", "true");
            }

            // Route based on user role
            if (result.User.Role == "ComplianceManager")
            {
                return RedirectToPage("/ComplianceManager/ComplianceDashboard");
            }
            else if (result.User.Role == "Employee")
            {
                return RedirectToPage("/Employee/Dashboard");
            }
            else
            {
                return RedirectToPage("/Admin/AdminDashboard");
            }
        }
    }
}