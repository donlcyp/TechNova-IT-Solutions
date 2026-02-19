using System.Threading.Tasks;

namespace TechNova_IT_Solutions.Services.Interfaces
{
    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IEmailService
    {
        Task<EmailSendResult> SendEmailAsync(string to, string subject, string body);
    }
}
