using Microsoft.AspNetCore.DataProtection;
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
        private readonly ITimeLimitedDataProtector _protector;

        private const string RememberMeCookieName = "TN_Auth";

        public LoginModel(
            IAuthenticationService authService,
            ILogger<LoginModel> logger,
            IUserService userService,
            IDataProtectionProvider dataProtectionProvider)
        {
            _authService = authService;
            _logger      = logger;
            _userService = userService;
            _protector   = dataProtectionProvider
                               .CreateProtector("TechNova.RememberMe.v1")
                               .ToTimeLimitedDataProtector();
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

        public async Task<IActionResult> OnGetAsync()
        {
            ErrorMessage = null;

            // Auto-login if a valid Remember Me cookie is present
            var token = Request.Cookies[RememberMeCookieName];
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Unprotect throws if the token is expired or tampered
                    var payload = _protector.Unprotect(token);
                    if (int.TryParse(payload, out var rememberedUserId))
                    {
                        var user = await _userService.GetUserByIdAsync(rememberedUserId);
                        if (user != null
                            && string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase)
                            && user.Role != RoleNames.Supplier)
                        {
                            RestoreSession(user);
                            return RedirectToPage(GetDashboardPage(user.Role));
                        }
                    }
                }
                catch
                {
                    // Expired or invalid token — remove it so the user sees the login form
                    Response.Cookies.Delete(RememberMeCookieName);
                }
            }

            // Load admin contact details for the Contact Admin modal
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
                HttpContext.Session.SetString(SessionKeys.UserId,   result.User.UserId.ToString());
                HttpContext.Session.SetString(SessionKeys.UserEmail, result.User.Email);
                HttpContext.Session.SetString(SessionKeys.UserName,  $"{result.User.FirstName} {result.User.LastName}");
                HttpContext.Session.SetString(SessionKeys.UserRole,  result.User.Role);

                if (result.User.BranchId.HasValue)
                {
                    HttpContext.Session.SetString(SessionKeys.BranchId,   result.User.BranchId.Value.ToString());
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

                    // Set a 30-day encrypted persistent cookie so the user is auto-logged
                    // in on their next visit without re-entering credentials.
                    var protectedToken = _protector.Protect(
                        result.User.UserId.ToString(),
                        DateTimeOffset.UtcNow.AddDays(30));

                    Response.Cookies.Append(RememberMeCookieName, protectedToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure   = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Expires  = DateTimeOffset.UtcNow.AddDays(30)
                    });
                }
                else
                {
                    // Clear any existing remember-me cookie when the user logs in without it
                    Response.Cookies.Delete(RememberMeCookieName);
                }

                // Flag if the user must change their default password
                if (result.User.MustChangePassword)
                {
                    HttpContext.Session.SetString(SessionKeys.MustChangePassword, "true");
                }

                return RedirectToPage(GetDashboardPage(result.User.Role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected login error for {Email}.", Email);
                ErrorMessage = "Something went wrong while signing in. Please try again.";
                return Page();
            }
        }

        // Restores the session from a UserData record (used by the Remember Me auto-login path)
        private void RestoreSession(UserData user)
        {
            HttpContext.Session.SetString(SessionKeys.UserId,    user.UserId);
            HttpContext.Session.SetString(SessionKeys.UserEmail,  user.Email);
            HttpContext.Session.SetString(SessionKeys.UserName,   user.FullName);
            HttpContext.Session.SetString(SessionKeys.UserRole,   user.Role);
            HttpContext.Session.SetString(SessionKeys.RememberMe, "true");

            if (user.BranchId.HasValue)
            {
                HttpContext.Session.SetString(SessionKeys.BranchId,   user.BranchId.Value.ToString());
                HttpContext.Session.SetString(SessionKeys.BranchName, user.BranchName ?? string.Empty);
            }
            else
            {
                HttpContext.Session.Remove(SessionKeys.BranchId);
                HttpContext.Session.Remove(SessionKeys.BranchName);
            }
        }

        private static string GetDashboardPage(string role) => role switch
        {
            RoleNames.SuperAdmin             => "/SuperAdmin/Dashboard",
            RoleNames.SystemAdmin            => "/SystemAdmin/Dashboard",
            RoleNames.BranchAdmin            => "/BranchAdmin/Dashboard",
            RoleNames.ChiefComplianceManager => "/ChiefComplianceManager/ComplianceDashboard",
            RoleNames.ComplianceManager      => "/ComplianceManager/ComplianceDashboard",
            RoleNames.Employee               => "/Employee/Dashboard",
            _                                => "/Account/Login"
        };
    }
}



