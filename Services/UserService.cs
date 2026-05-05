using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public UserService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<UserData>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Branch)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new UserData
                {
                    UserId    = u.UserId.ToString(),
                    FirstName = u.FirstName ?? string.Empty,
                    LastName  = u.LastName  ?? string.Empty,
                    Email     = u.Email     ?? string.Empty,
                    Role      = u.Role      ?? string.Empty,
                    Status    = u.Status    ?? "Active",
                    BranchId  = u.BranchId,
                    BranchName = u.Branch != null ? u.Branch.BranchName : null
                })
                .ToListAsync();
        }

        public async Task<UserData?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return null;

            return new UserData
            {
                UserId    = user.UserId.ToString(),
                FirstName = user.FirstName ?? string.Empty,
                LastName  = user.LastName  ?? string.Empty,
                Email     = user.Email     ?? string.Empty,
                Role      = user.Role      ?? string.Empty,
                Status    = user.Status    ?? "Active",
                BranchId  = user.BranchId,
                BranchName = user.Branch?.BranchName
            };
        }

        public async Task<UserCreationResult> CreateUserAsync(UserData userData)
        {
            if (userData == null)
            {
                return new UserCreationResult { Success = false, ErrorMessage = "Invalid user data." };
            }

            var firstName = userData.FirstName?.Trim();
            var lastName = userData.LastName?.Trim();
            var email = userData.Email?.Trim();
            var role = userData.Role?.Trim();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(role))
            {
                return new UserCreationResult
                {
                    Success = false,
                    ErrorMessage = "First name, last name, email, and role are required."
                };
            }

            var normalizedEmail = email.ToLowerInvariant();
            var emailExists = await _context.Users.AnyAsync(u =>
                u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);
            if (emailExists)
            {
                return new UserCreationResult { Success = false, ErrorMessage = "Email already exists." };
            }

            // Generate a secure random password instead of using hardcoded defaults
            var generatedPassword = SecurePasswordService.GenerateSecurePassword();
            var hashedPassword = PasswordHasher.HashPassword(generatedPassword);

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Role = role,
                Status = string.IsNullOrWhiteSpace(userData.Status) ? "Active" : userData.Status,
                Password = hashedPassword,
                MustChangePassword = true,
                BranchId = userData.BranchId
            };

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return new UserCreationResult { Success = false, ErrorMessage = "Email already exists." };
            }
            catch (DbUpdateException)
            {
                return new UserCreationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create user. Check required fields and data length limits."
                };
            }
            catch
            {
                return new UserCreationResult { Success = false, ErrorMessage = "Failed to create user." };
            }

            var result = new UserCreationResult { Success = true };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                result.EmailAttempted = true;

                try
                {
                    // Generate a password reset token and send secure link instead of password
                    var resetToken = GeneratePasswordResetToken(user.UserId);
                    if (string.IsNullOrWhiteSpace(resetToken))
                    {
                        result.EmailSent = false;
                        result.EmailError = "Password reset token could not be generated.";
                        return result;
                    }

                    var roleLabel = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
                    var subject = $"Your TechNova {roleLabel} Account Has Been Created - Set Your Password";
                    var resetLink = $"https://yourdomain.com/account/reset-password?token={Uri.EscapeDataString(resetToken)}";
                    var body = $@"
                        <h2>Welcome to TechNova</h2>
                        <p>Hello {user.FirstName},</p>
                        <p>Your account has been created successfully.</p>
                        <p><strong>Role:</strong> {System.Net.WebUtility.HtmlEncode(roleLabel)}</p>
                        <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(user.Email)}</p>
                        <p style=""margin: 20px 0;""><a href=""{resetLink}"" style=""background-color: #1e40af; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;"">Set Your Password</a></p>
                        <p>This link expires in 24 hours.</p>
                        <p>If you did not request this account, please contact support immediately.</p>";

                    var emailResult = await _emailService.SendEmailAsync(user.Email, subject, body);
                    result.EmailSent = emailResult.Success;
                    result.EmailError = emailResult.ErrorMessage;
                }
                catch (DbUpdateException ex) when (IsMissingPasswordResetTokensTable(ex))
                {
                    result.EmailSent = false;
                    result.EmailError = "Password reset token storage is not available. Run the latest database migration.";
                }
                catch
                {
                    result.EmailSent = false;
                    result.EmailError = "Password reset token could not be generated.";
                }
            }

            return result;
        }

        public async Task<bool> UpdateUserAsync(UserData userData)
        {
            try
            {
                if (!int.TryParse(userData.UserId, out int userId))
                    return false;

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.FirstName = userData.FirstName;
                user.LastName  = userData.LastName;
                user.Email     = userData.Email;
                user.Role      = userData.Role;
                user.Status    = userData.Status;
                user.BranchId  = userData.BranchId;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.Status = "Inactive";
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReactivateUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.Status = "Active";
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PasswordResetResult> ResetPasswordByRoleAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = "User not found."
                    };
                }

                // Generate secure random password instead of role-based defaults
                var resetPassword = SecurePasswordService.GenerateSecurePassword();
                user.Password = PasswordHasher.HashPassword(resetPassword);
                user.MustChangePassword = true;
                await _context.SaveChangesAsync();

                return new PasswordResetResult
                {
                    Success = true,
                    Password = resetPassword,
                    Role = user.Role ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? "User"
                };
            }
            catch
            {
                return new PasswordResetResult
                {
                    Success = false,
                    ErrorMessage = "Failed to reset password."
                };
            }
        }

        public async Task<bool> SetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.Password = PasswordHasher.HashPassword(newPassword);
                user.MustChangePassword = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ClearMustChangePasswordAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.MustChangePassword = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a secure password reset token valid for 24 hours.
        /// </summary>
        private string GeneratePasswordResetToken(int userId)
        {
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{userId}-{DateTime.UtcNow:O}-{Guid.NewGuid()}"));
            var resetToken = new PasswordResetToken
            {
                UserId = userId,
                Token = token,
                ExpiryDate = DateTime.UtcNow.AddHours(24),
                CreatedDate = DateTime.UtcNow,
                IsUsed = false
            };
            
            _context.PasswordResetTokens.Add(resetToken);
            _context.SaveChanges();
            
            return token;
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number == 2601 || sqlEx.Number == 2627;
            }

            return false;
        }

        private static bool IsMissingPasswordResetTokensTable(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number == 208 &&
                       sqlEx.Message.IndexOf("PasswordResetTokens", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }
    }
}
