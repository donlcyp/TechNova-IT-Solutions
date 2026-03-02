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
            try
            {
                // Assign role-based default password
                var role = userData.Role ?? string.Empty;
                string passwordToHash;
                if (string.Equals(role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                    passwordToHash = "superadmin123";
                else if (string.Equals(role, RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase))
                    passwordToHash = "systemadmin123";
                else if (string.Equals(role, RoleNames.BranchAdmin, StringComparison.OrdinalIgnoreCase))
                    passwordToHash = "branchadmin123";
                else if (string.Equals(role, RoleNames.ChiefComplianceManager, StringComparison.OrdinalIgnoreCase))
                    passwordToHash = "chiefcompliance123";
                else if (role.Contains("Compliance", StringComparison.OrdinalIgnoreCase))
                    passwordToHash = "compliance123";
                else if (role.Contains("Supplier", StringComparison.OrdinalIgnoreCase))
                    passwordToHash = "supplier123";
                else
                    passwordToHash = "employee123";

                var hashedPassword = PasswordHasher.HashPassword(passwordToHash);
                
                var user = new User
                {
                    FirstName = userData.FirstName,
                    LastName  = userData.LastName,
                    Email     = userData.Email,
                    Role      = userData.Role ?? string.Empty,
                    Status    = userData.Status,
                    Password  = hashedPassword,
                    MustChangePassword = true,
                    BranchId  = userData.BranchId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var result = new UserCreationResult { Success = true };

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var roleLabel = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
                    var subject = $"Your TechNova {roleLabel} Account Has Been Created";
                    var body = $@"
                        <h2>Welcome to TechNova</h2>
                        <p>Your account has been created.</p>
                        <p><strong>Role:</strong> {roleLabel}</p>
                        <p><strong>Email:</strong> {user.Email}</p>
                        <p><strong>Temporary Password:</strong> {passwordToHash}</p>
                        <p>Please log in and change your password immediately.</p>";

                    result.EmailAttempted = true;
                    var emailResult = await _emailService.SendEmailAsync(user.Email, subject, body);
                    result.EmailSent = emailResult.Success;
                    result.EmailError = emailResult.ErrorMessage;
                }

                return result;
            }
            catch
            {
                return new UserCreationResult { Success = false };
            }
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

                var role = user.Role ?? string.Empty;
                string resetPassword;

                if (string.Equals(role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    resetPassword = "superadmin123";
                }
                else if (string.Equals(role, RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    resetPassword = "systemadmin123";
                }
                else if (string.Equals(role, RoleNames.BranchAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    resetPassword = "branchadmin123";
                }
                else if (string.Equals(role, RoleNames.ChiefComplianceManager, StringComparison.OrdinalIgnoreCase))
                {
                    resetPassword = "chiefcompliance123";
                }
                else if (role.Contains("Compliance", StringComparison.OrdinalIgnoreCase))
                {
                    resetPassword = "compliance123";
                }
                else if (role.Contains("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    resetPassword = "employee123";
                }
                else if (role.Contains("Supplier", StringComparison.OrdinalIgnoreCase))
                {
                    resetPassword = "supplier123";
                }
                else
                {
                    resetPassword = "TempPassword123!";
                }

                user.Password = PasswordHasher.HashPassword(resetPassword);
                user.MustChangePassword = true;
                await _context.SaveChangesAsync();

                return new PasswordResetResult
                {
                    Success = true,
                    Password = resetPassword,
                    Role = role,
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
    }
}
