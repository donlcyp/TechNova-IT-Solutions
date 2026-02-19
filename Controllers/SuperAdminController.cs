using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class SuperAdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public SuperAdminController(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserData userData)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
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
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

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
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }

            var result = await _userService.UpdateUserAsync(userData);
            if (result)
            {
                return Ok(new { success = true, message = "User updated successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to update user" });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
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
                        <p>Your TechNova account has been <strong>deactivated</strong> by Super Admin.</p>
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
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
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
                        <p>Your TechNova account has been <strong>reactivated</strong> by Super Admin.</p>
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

        [HttpPost]
        public async Task<IActionResult> ResetUserPassword(int userId)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
            if (string.Equals(user.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Super Admin accounts are protected and cannot be reset." });
            }

            var resetResult = await _userService.ResetPasswordByRoleAsync(userId);
            if (!resetResult.Success)
            {
                return BadRequest(new { success = false, message = resetResult.ErrorMessage });
            }

            var emailAttempted = false;
            var emailSent = false;
            string? emailError = null;

            if (!string.IsNullOrWhiteSpace(resetResult.Email))
            {
                emailAttempted = true;
                var subject = "TechNova Password Reset";
                var body = $@"
                    <h2>Password Reset Completed</h2>
                    <p>Hello {resetResult.FirstName},</p>
                    <p>Your account password has been reset by Super Admin.</p>
                    <p><strong>Role:</strong> {resetResult.Role}</p>
                    <p><strong>Temporary Password:</strong> {resetResult.Password}</p>
                    <p>Please log in and change your password immediately.</p>";

                var emailResult = await _emailService.SendEmailAsync(resetResult.Email, subject, body);
                emailSent = emailResult.Success;
                emailError = emailResult.ErrorMessage;
            }

            var message = $"Password reset successfully to role default ({resetResult.Password}).";
            if (emailAttempted)
            {
                message = emailSent
                    ? $"{message} Email notification sent."
                    : $"{message} Email failed: {emailError ?? "Unknown error"}";
            }

            return Ok(new
            {
                success = true,
                message,
                resetPassword = resetResult.Password,
                emailAttempted,
                emailSent,
                emailError
            });
        }
    }
}
