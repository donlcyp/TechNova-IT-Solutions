using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Services.Interfaces;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.RateLimiting;

namespace TechNova_IT_Solutions.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserService _userService;
        private readonly IAdminService _adminService;

        // Blacklist of known weak passwords
        private static readonly HashSet<string> WeakPasswordBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin@123",
            "Password123!",
            "Welcome123!",
            "Passw0rd!",
            "P@ssw0rd",
            "Admin123!",
            "Password1!",
            "Qwerty123!",
            "Letmein123!",
            "Welcome1!"
        };

        public AccountController(IAuthenticationService authService, IUserService userService, IAdminService adminService)
        {
            _authService = authService;
            _userService = userService;
            _adminService = adminService;
        }

        /// <summary>
        /// Validates password complexity requirements
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if password meets all requirements, false otherwise</returns>
        private bool ValidatePasswordComplexity(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Check minimum length (12 characters)
            if (password.Length < 12)
            {
                errorMessage = "Password must be at least 12 characters long.";
                return false;
            }

            // Check for at least one uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errorMessage = "Password must contain at least one uppercase letter (A-Z).";
                return false;
            }

            // Check for at least one lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errorMessage = "Password must contain at least one lowercase letter (a-z).";
                return false;
            }

            // Check for at least one digit
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errorMessage = "Password must contain at least one digit (0-9).";
                return false;
            }

            // Check for at least one special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*()\-_=+\[\]{}|;:,.<>?]"))
            {
                errorMessage = "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?).";
                return false;
            }

            // Check against blacklist of known weak passwords
            if (WeakPasswordBlacklist.Contains(password))
            {
                errorMessage = "This password is known to be weak and commonly used. Please choose a different password.";
                return false;
            }

            return true;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to appropriate dashboard
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (!string.IsNullOrEmpty(userRole))
            {
                return RedirectToDashboard(userRole);
            }

            return View();
        }

        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Please enter both email and password.";
                return View();
            }

            var result = await _authService.AuthenticateUserAsync(email, password);

            if (!result.Success || result.User == null)
            {
                ViewBag.ErrorMessage = result.ErrorMessage ?? "Invalid email or password.";
                return View();
            }

            var user = result.User;

            // Store user information in session
            HttpContext.Session.SetString(SessionKeys.UserId, user.UserId.ToString());
            HttpContext.Session.SetString(SessionKeys.UserRole, user.Role ?? RoleNames.Employee);
            HttpContext.Session.SetString(SessionKeys.UserEmail, user.Email);
            HttpContext.Session.SetString(SessionKeys.UserName, $"{user.FirstName} {user.LastName}");

            if (user.BranchId.HasValue)
            {
                HttpContext.Session.SetString(SessionKeys.BranchId, user.BranchId.Value.ToString());
                HttpContext.Session.SetString(SessionKeys.BranchName, user.Branch?.BranchName ?? string.Empty);
            }
            else
            {
                HttpContext.Session.Remove(SessionKeys.BranchId);
                HttpContext.Session.Remove(SessionKeys.BranchName);
            }

            // Redirect based on role
            return RedirectToDashboard(user.Role ?? RoleNames.Employee);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            ViewBag.Message = "You do not have permission to access this resource.";
            return View();
        }

        private IActionResult RedirectToDashboard(string role)
        {
            role = role?.Trim() ?? string.Empty;

            if (role.Equals(RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/SuperAdmin/Dashboard");

            if (role.Equals(RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/SystemAdmin/Dashboard");

            if (role.Equals(RoleNames.BranchAdmin, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/BranchAdmin/Dashboard");

            if (role.Equals(RoleNames.ChiefComplianceManager, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/ChiefComplianceManager/ComplianceDashboard");

            if (role.Equals(RoleNames.ComplianceManager, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "ComplianceManager");

            if (role.Equals(RoleNames.Supplier, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/Supplier/Dashboard");

            if (role.Equals(RoleNames.Employee, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Employee");

            return RedirectToAction("Dashboard", "Employee");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdStr = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { success = false, message = "Not authenticated." });
            }

            if (string.IsNullOrWhiteSpace(request?.NewPassword))
            {
                return BadRequest(new { success = false, message = "Password cannot be empty." });
            }

            // Validate password complexity
            if (!ValidatePasswordComplexity(request.NewPassword, out string errorMessage))
            {
                return BadRequest(new { success = false, message = errorMessage });
            }

            var result = await _userService.SetPasswordAsync(userId, request.NewPassword);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to change password." });
            }

            // Add audit log for password change by user
            await _adminService.LogActivityAsync(userId, "Password changed by user", "Authentication");

            // Clear forced password change flag
            HttpContext.Session.Remove(SessionKeys.MustChangePassword);

            return Ok(new { success = true, message = "Password changed successfully." });
        }
    }
}

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
