using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserData>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new UserData
                {
                    UserId = u.UserId.ToString(),
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Role = u.Role ?? string.Empty,
                    Status = u.Status ?? "Active"
                })
                .ToListAsync();
        }

        public async Task<UserData?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserData
            {
                UserId = user.UserId.ToString(),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role ?? string.Empty,
                Status = user.Status ?? "Active"
            };
        }

        public async Task<bool> CreateUserAsync(UserData userData)
        {
            try
            {
                // Use provided password or default if not provided
                var passwordToHash = string.IsNullOrWhiteSpace(userData.Password) 
                    ? "TempPassword123!" 
                    : userData.Password;
                var hashedPassword = PasswordHasher.HashPassword(passwordToHash);
                
                var user = new User
                {
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    Email = userData.Email,
                    Role = userData.Role,
                    Status = userData.Status,
                    Password = hashedPassword
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
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
                user.LastName = userData.LastName;
                user.Email = userData.Email;
                user.Role = userData.Role;
                user.Status = userData.Status;

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
    }
}
