using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<LoginModel> _logger;
        private readonly IUserService _userService;

        public LoginModel(IAuthenticationService authService, ILogger<LoginModel> logger, IUserService userService)
        {
            _authService = authService;
            _logger      = logger;
            _userService = userService;
        }

        // Admin contacts for the Contact Admin modal
        public string SuperAdminName  { get; set; } = "Super Administrator";
        public string SuperAdminEmail { get; set; } = "superadmin@technova.com";
        public string SysAdminName    { get; set; } = "System Administrator";
        public string SysAdminEmail   { get; set; } = "sysadmin@technova.com";

        [BindProperty]
        [Required(ErrorMessage = "Company email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid company email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public bool ShowSupplierModal { get; private set; } = false;

        public async Task OnGetAsync()
        {
            ErrorMessage = null;

            try
            {
                var allUsers = await _userService.GetAllUsersAsync();

                var superAdmin = allUsers.FirstOrDefault(u => u.Role == RoleNames.SuperAdmin);
                if (superAdmin != null)
                {
                    SuperAdminName  = superAdmin.FullName;
                    SuperAdminEmail = superAdmin.Email;
                }

                var sysAdmin = allUsers.FirstOrDefault(u => u.Role == RoleNames.SystemAdmin);
                if (sysAdmin != null)
                {
                    SysAdminName  = sysAdmin.FullName;
                    SysAdminEmail = sysAdmin.Email;
                }
            }
            catch { /* fall back to defaults */ }
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
                    ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Invalid email or password."
                        : result.ErrorMessage;
                    return Page();
                }

                // Block suppliers from the employee portal before any session is written
                if (result.User.Role == RoleNames.Supplier)
                {
                    ShowSupplierModal = true;
                    return Page();
                }

                // Store user info in Session
                HttpContext.Session.SetString(SessionKeys.UserId,  result.User.UserId.ToString());
                HttpContext.Session.SetString(SessionKeys.UserEmail, result.User.Email);
                HttpContext.Session.SetString(SessionKeys.UserName, $"{result.User.FirstName} {result.User.LastName}");
                HttpContext.Session.SetString(SessionKeys.UserRole, result.User.Role);

                if (result.User.BranchId.HasValue)
                {
                    HttpContext.Session.SetString(SessionKeys.BranchId, result.User.BranchId.Value.ToString());
                    HttpContext.Session.SetString(SessionKeys.BranchName, result.User.Branch?.BranchName ?? string.Empty);
                }
                else
                {
                    HttpContext.Session.Remove(SessionKeys.BranchId);
                    HttpContext.Session.Remove(SessionKeys.BranchName);
                }

                if (RememberMe)
                {
                    HttpContext.Session.SetString(SessionKeys.RememberMe, "true");
                }

                // Flag if the user must change their default password
                if (result.User.MustChangePassword)
                {
                    HttpContext.Session.SetString(SessionKeys.MustChangePassword, "true");
                }

                // Route based on user role
                if (result.User.Role == RoleNames.SuperAdmin)
                {
                    return RedirectToPage("/SuperAdmin/Dashboard");
                }
                else if (result.User.Role == RoleNames.SystemAdmin)
                {
                    return RedirectToPage("/SystemAdmin/Dashboard");
                }
                else if (result.User.Role == RoleNames.BranchAdmin)
                {
                    return RedirectToPage("/BranchAdmin/Dashboard");
                }
                else if (result.User.Role == RoleNames.ChiefComplianceManager)
                {
                    return RedirectToPage("/ChiefComplianceManager/ComplianceDashboard");
                }
                else if (result.User.Role == RoleNames.ComplianceManager)
                {
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                }
                else if (result.User.Role == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                else
                {
                    return RedirectToPage("/Account/Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected login error for {Email}.", Email);
                ErrorMessage = "Something went wrong while signing in. Please try again.";
                return Page();
            }
        }
    }
}



