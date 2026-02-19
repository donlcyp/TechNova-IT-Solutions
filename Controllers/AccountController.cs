using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;

        public AccountController(IAuthenticationService authService)
        {
            _authService = authService;
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

            if (role.Equals(RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Admin");

            if (role.Equals(RoleNames.ComplianceManager, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "ComplianceManager");

            if (role.Equals(RoleNames.Supplier, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/Supplier/Dashboard");

            if (role.Equals(RoleNames.Employee, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Employee");

            return RedirectToAction("Dashboard", "Employee");
        }
    }
}
