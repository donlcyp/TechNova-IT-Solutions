using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const int MaxFailedAttempts = 3;
        private const int LockoutMinutes = 15;
        private const string LockoutCachePrefix = "login_lockout_";

        public AuthenticationService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<AuthenticationResult> AuthenticateUserAsync(string email, string password)
        {
            try
            {
                // Trim email only; passwords are case- and space-sensitive
                email = email?.Trim() ?? string.Empty;

                // Validate inputs
                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Please enter your email and password." };
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Please enter your email address." };
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Please enter your password." };
                }

                // Validate email format
                if (!IsValidEmail(email))
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Please enter a valid email address." };
                }

                // Check for lockout
                if (IsAccountLockedOut(email))
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Too many failed login attempts. Please try again later." };
                }

                // Find user and include Branch for session population
                var user = await _context.Users
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user == null)
                {
                    RecordFailedAttempt(email);
                    return new AuthenticationResult { Success = false, ErrorMessage = "Invalid email or password. Please try again." };
                }

                // Verify password using BCrypt
                if (!PasswordHasher.VerifyPassword(password, user.Password))
                {
                    RecordFailedAttempt(email);
                    var remainingAttempts = GetRemainingAttempts(email);
                    
                    var errorMsg = remainingAttempts > 0 
                        ? "Invalid email or password. Please try again." 
                        : "Too many failed login attempts. Please try again later.";
                    
                    return new AuthenticationResult { Success = false, ErrorMessage = errorMsg };
                }

                // Check account status
                if (user.Status != "Active")
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Your account is inactive. Please contact the system administrator." };
                }

                // Success - clear failed attempts
                ClearFailedAttempts(email);

                // Log the login
                var auditLog = new AuditLog
                {
                    UserId = user.UserId,
                    Action = "User Login",
                    Module = "Authentication",
                    LogDate = DateTime.Now
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                return new AuthenticationResult { Success = true, User = user };
            }
            catch
            {
                return new AuthenticationResult { Success = false, ErrorMessage = "An error occurred during login. Please try again or contact your administrator." };
            }
        }

        public async Task<bool> LogoutUserAsync(int userId)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = "User Logout",
                    Module = "Authentication",
                    LogDate = DateTime.Now
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsAccountLockedOut(string email)
        {
            var key = LockoutCachePrefix + email.ToLower();
            if (_cache.TryGetValue(key, out LoginAttempt? attempt) && attempt != null)
            {
                return attempt.FailedAttempts >= MaxFailedAttempts;
            }
            return false;
        }

        public void ClearFailedAttempts(string email)
        {
            _cache.Remove(LockoutCachePrefix + email.ToLower());
        }

        private void RecordFailedAttempt(string email)
        {
            var key = LockoutCachePrefix + email.ToLower();
            var attempt = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LockoutMinutes);
                return new LoginAttempt { FailedAttempts = 0, LastAttemptTime = DateTime.Now };
            })!;

            attempt.FailedAttempts++;
            attempt.LastAttemptTime = DateTime.Now;

            // Refresh cache so it persists in IMemoryCache
            _cache.Set(key, attempt, TimeSpan.FromMinutes(LockoutMinutes));
        }

        private int GetRemainingAttempts(string email)
        {
            var key = LockoutCachePrefix + email.ToLower();
            if (_cache.TryGetValue(key, out LoginAttempt? attempt) && attempt != null)
            {
                return Math.Max(0, MaxFailedAttempts - attempt.FailedAttempts);
            }
            return MaxFailedAttempts;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class LoginAttempt
    {
        public int FailedAttempts { get; set; }
        public DateTime LastAttemptTime { get; set; }
    }
}
