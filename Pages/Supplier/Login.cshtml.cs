using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.Supplier
{
    public class LoginModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IAuthenticationService authService, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Supplier email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid supplier email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            ErrorMessage = null;

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (!string.IsNullOrWhiteSpace(userRole) && userRole.Equals(RoleNames.Supplier, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Supplier/Dashboard");
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            Email = Email?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the highlighted fields and try again.";
                return Page();
            }

            try
            {
                var result = await _authService.AuthenticateUserAsync(Email, Password);
                if (!result.Success || result.User == null)
                {
                    ErrorMessage = result.ErrorMessage ?? "Invalid email or password.";
                    return Page();
                }

                if (!string.Equals(result.User.Role, RoleNames.Supplier, StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "Please use the Company Sign In page.";
                    return Page();
                }

                HttpContext.Session.SetString(SessionKeys.UserId, result.User.UserId.ToString());
                HttpContext.Session.SetString(SessionKeys.UserEmail, result.User.Email);
                HttpContext.Session.SetString(SessionKeys.UserName, $"{result.User.FirstName} {result.User.LastName}");
                HttpContext.Session.SetString(SessionKeys.UserRole, result.User.Role);

                if (RememberMe)
                {
                    HttpContext.Session.SetString(SessionKeys.RememberMe, "true");
                }

                return RedirectToPage("/Supplier/Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected supplier login error for {Email}.", Email);
                ErrorMessage = "Something went wrong while signing in. Please try again.";
                return Page();
            }
        }
    }
}



