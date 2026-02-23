using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminUserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public AdminUserController(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return userRole == RoleNames.Admin || userRole == RoleNames.SuperAdmin;
        }

        private bool IsCurrentUserSuperAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return string.Equals(userRole, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrivilegedAdminRole(string? role)
        {
            return string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserData userData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }
            if (!IsCurrentUserSuperAdmin() && IsPrivilegedAdminRole(userData.Role))
            {
                return BadRequest(new { success = false, message = "System Administrator cannot create Admin or Super Admin accounts." });
            }

            var result = await _userService.CreateUserAsync(userData);

            if (result.Success)
            {
                var message = "User created successfully.";
                if (result.EmailAttempted)
                {
                    message = result.EmailSent
                        ? "User created successfully. Account email was sent."
                        : $"User created successfully, but account email failed: {result.EmailError ?? "Unknown error"}";
                }

                return Ok(new
                {
                    success = true,
                    message,
                    emailAttempted = result.EmailAttempted,
                    emailSent = result.EmailSent,
                    emailError = result.EmailError
                });
            }

            return BadRequest(new { success = false, message = "Failed to create user" });
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var user = await _userService.GetUserByIdAsync(userId);

            if (user != null)
            {
                return Ok(new { success = true, user });
            }

            return NotFound(new { success = false, message = "User not found" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UserData userData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }
            if (!IsCurrentUserSuperAdmin() && IsPrivilegedAdminRole(userData.Role))
            {
                return BadRequest(new { success = false, message = "System Administrator cannot assign Admin or Super Admin roles." });
            }

            var result = await _userService.UpdateUserAsync(userData);

            if (result)
            {
                return Ok(new { success = true, message = "User updated successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to update user" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _userService.DeleteUserAsync(userId);

            if (result)
            {
                return Ok(new { success = true, message = "User deleted successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to delete user" });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });
            if (string.Equals(user.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Super Admin accounts are protected and cannot be deactivated." });
            }

            var result = await _userService.DeactivateUserAsync(userId);

            if (result)
            {
                var emailAttempted = false;
                var emailSent = false;
                string? emailError = null;

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    emailAttempted = true;
                    var subject = "TechNova Account Deactivated";
                    var body = $@"
                        <h2>Account Status Update</h2>
                        <p>Hello {user.FirstName},</p>
                        <p>Your TechNova account has been <strong>deactivated</strong> by an administrator.</p>
                        <p>If you think this is a mistake, contact your system administrator.</p>";

                    var emailResult = await _emailService.SendEmailAsync(user.Email, subject, body);
                    emailSent = emailResult.Success;
                    emailError = emailResult.ErrorMessage;
                }

                var message = "User deactivated successfully.";
                if (emailAttempted)
                {
                    message = emailSent
                        ? "User deactivated successfully. Email notification sent."
                        : $"User deactivated successfully, but email failed: {emailError ?? "Unknown error"}";
                }

                return Ok(new
                {
                    success = true,
                    message,
                    emailAttempted,
                    emailSent,
                    emailError
                });
            }

            return BadRequest(new { success = false, message = "Failed to deactivate user" });
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });
            if (string.Equals(user.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Super Admin accounts are protected and cannot be reactivated via this action." });
            }

            var result = await _userService.ReactivateUserAsync(userId);

            if (result)
            {
                var emailAttempted = false;
                var emailSent = false;
                string? emailError = null;

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    emailAttempted = true;
                    var subject = "TechNova Account Reactivated";
                    var body = $@"
                        <h2>Account Status Update</h2>
                        <p>Hello {user.FirstName},</p>
                        <p>Your TechNova account has been <strong>reactivated</strong> by an administrator.</p>
                        <p>You can now sign in again.</p>";

                    var emailResult = await _emailService.SendEmailAsync(user.Email, subject, body);
                    emailSent = emailResult.Success;
                    emailError = emailResult.ErrorMessage;
                }

                var message = "User reactivated successfully.";
                if (emailAttempted)
                {
                    message = emailSent
                        ? "User reactivated successfully. Email notification sent."
                        : $"User reactivated successfully, but email failed: {emailError ?? "Unknown error"}";
                }

                return Ok(new
                {
                    success = true,
                    message,
                    emailAttempted,
                    emailSent,
                    emailError
                });
            }

            return BadRequest(new { success = false, message = "Failed to reactivate user" });
        }
    }
}
