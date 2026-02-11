using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateUserAsync(string email, string password);
        Task<bool> LogoutUserAsync(int userId);
        bool IsAccountLockedOut(string email);
        void ClearFailedAttempts(string email);
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
    }
}
