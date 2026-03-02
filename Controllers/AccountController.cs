using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserService _userService;

        public AccountController(IAuthenticationService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
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

            if (string.IsNullOrWhiteSpace(request?.NewPassword) || request.NewPassword.Length < 8)
            {
                return BadRequest(new { success = false, message = "Password must be at least 8 characters." });
            }

            var result = await _userService.SetPasswordAsync(userId, request.NewPassword);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to change password." });
            }

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
