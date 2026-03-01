namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserData>> GetAllUsersAsync();
        Task<UserData?> GetUserByIdAsync(int userId);
        Task<UserCreationResult> CreateUserAsync(UserData userData);
        Task<bool> UpdateUserAsync(UserData userData);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ReactivateUserAsync(int userId);
        Task<PasswordResetResult> ResetPasswordByRoleAsync(int userId);
        Task<bool> SetPasswordAsync(int userId, string newPassword);
        Task<bool> ClearMustChangePasswordAsync(int userId);
    }

    public class UserCreationResult
    {
        public bool Success { get; set; }
        public bool EmailAttempted { get; set; }
        public bool EmailSent { get; set; }
        public string? EmailError { get; set; }
    }

    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class UserData
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}
