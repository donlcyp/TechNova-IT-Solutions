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
            return RoleNames.IsAdminRole(userRole) || userRole == RoleNames.SuperAdmin;
        }

        private bool IsCurrentUserSuperAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return string.Equals(userRole, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrivilegedAdminRole(string? role)
        {
            return RoleNames.IsAdminRole(role) ||
                   string.Equals(role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        private int? GetCallerBranchId()
        {
            var s = HttpContext.Session.GetString(SessionKeys.BranchId);
            return int.TryParse(s, out var id) ? id : null;
        }

        /// <summary>
        /// Branch Admins can only manage users in their own branch.
        /// SuperAdmin can manage any user.
        /// Returns null if access is allowed, or an IActionResult to return if denied.
        /// </summary>
        private async Task<IActionResult?> EnforceBranchScopeAsync(int targetUserId)
        {
            if (IsCurrentUserSuperAdmin()) return null;

            var callerBranchId = GetCallerBranchId();
            if (!callerBranchId.HasValue)
                return BadRequest(new { success = false, message = "You have no branch assigned." });

            var targetUser = await _userService.GetUserByIdAsync(targetUserId);
            if (targetUser == null)
                return NotFound(new { success = false, message = "User not found" });

            if (targetUser.BranchId != callerBranchId.Value)
                return Unauthorized(new { success = false, message = "You can only manage users within your branch." });

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserData userData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }
            // SuperAdmin can create any role. SystemAdmin can create BranchAdmin but not SystemAdmin/SuperAdmin.
            if (!IsCurrentUserSuperAdmin())
            {
                if (string.Equals(userData.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(userData.Role, RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "System Administrator cannot create System Admin or Super Admin accounts." });
                }
            }

            // If the calling user is an Admin (not SuperAdmin) and no branch was explicitly provided,
            // automatically attach the caller's branch
            if (!IsCurrentUserSuperAdmin() && !userData.BranchId.HasValue)
            {
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (int.TryParse(branchIdStr, out int callerBranchId))
                    userData.BranchId = callerBranchId;
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

            // Branch Admin can only view users in their branch
            var branchDenied = await EnforceBranchScopeAsync(userId);
            if (branchDenied != null) return branchDenied;

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
            if (!IsCurrentUserSuperAdmin())
            {
                if (string.Equals(userData.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(userData.Role, RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "System Administrator cannot assign System Admin or Super Admin roles." });
                }
            }

            // Branch Admin can only update users in their branch
            if (int.TryParse(userData.UserId, out var uid))
            {
                var branchDenied = await EnforceBranchScopeAsync(uid);
                if (branchDenied != null) return branchDenied;
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

            // Branch Admin can only delete users in their branch
            var branchDenied = await EnforceBranchScopeAsync(userId);
            if (branchDenied != null) return branchDenied;

            var result = await _userService.DeleteUserAsync(userId);

            if (result)
            {
                return Ok(new { success = true, message = "User deleted successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to delete user" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetUserPassword(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            // Branch Admin can only reset passwords for users in their branch
            var branchDenied = await EnforceBranchScopeAsync(userId);
            if (branchDenied != null) return branchDenied;

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });

            if (!IsCurrentUserSuperAdmin() && IsPrivilegedAdminRole(user.Role))
            {
                return BadRequest(new { success = false, message = "Cannot reset password for Admin or Super Admin accounts." });
            }

            if (string.Equals(user.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Super Admin accounts are protected." });
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
                    <p>Your account password has been reset by an administrator.</p>
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
                emailAttempted,
                emailSent,
                emailError
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            // Branch Admin can only deactivate users in their branch
            var branchDenied = await EnforceBranchScopeAsync(userId);
            if (branchDenied != null) return branchDenied;

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

            // Branch Admin can only reactivate users in their branch
            var branchDenied = await EnforceBranchScopeAsync(userId);
            if (branchDenied != null) return branchDenied;

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

        [HttpPost]
        public async Task<IActionResult> SetUserPassword(int userId, [FromBody] SetPasswordRequest request)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            // Branch Admin can only set passwords for users in their branch
            var branchDenied = await EnforceBranchScopeAsync(userId);
            if (branchDenied != null) return branchDenied;

            if (string.IsNullOrWhiteSpace(request?.NewPassword) || request.NewPassword.Length < 8)
            {
                return BadRequest(new { success = false, message = "Password must be at least 8 characters." });
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
            if (IsPrivilegedAdminRole(user.Role) && !IsCurrentUserSuperAdmin())
            {
                return BadRequest(new { success = false, message = "Cannot reset password for Admin/Super Admin accounts." });
            }

            var result = await _userService.SetPasswordAsync(int.Parse(user.UserId), request.NewPassword);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to set password." });
            }

            var emailAttempted = false;
            var emailSent = false;
            string? emailError = null;

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                emailAttempted = true;
                var subject = "TechNova Password Changed";
                var body = $@"
                    <h2>Password Changed</h2>
                    <p>Hello {user.FirstName},</p>
                    <p>Your account password has been changed by an administrator.</p>
                    <p>If you did not request this change, contact your system administrator immediately.</p>";

                var emailResult = await _emailService.SendEmailAsync(user.Email, subject, body);
                emailSent = emailResult.Success;
                emailError = emailResult.ErrorMessage;
            }

            var message = "Password updated successfully.";
            if (emailAttempted)
            {
                message = emailSent
                    ? $"{message} Email notification sent."
                    : $"{message} Email failed: {emailError ?? "Unknown error"}";
            }

            return Ok(new { success = true, message });
        }
    }
}
