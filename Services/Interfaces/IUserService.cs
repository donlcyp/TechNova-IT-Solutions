namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserData>> GetAllUsersAsync();
        Task<UserData?> GetUserByIdAsync(int userId);
        Task<bool> CreateUserAsync(UserData userData);
        Task<bool> UpdateUserAsync(UserData userData);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ReactivateUserAsync(int userId);
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
        public string FullName => $"{FirstName} {LastName}";
    }
}
