using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using TechNova_IT_Solutions.Services.Interfaces;
using System.Threading.Tasks;

namespace TechNova_IT_Solutions.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var host = emailSettings["Host"];
            var port = int.Parse(emailSettings["Port"] ?? "587");
            var username = emailSettings["Username"];
            var password = emailSettings["Password"];
            var fromEmail = emailSettings["FromEmail"] ?? username;
            var useSsl = bool.TryParse(emailSettings["UseSsl"], out var parsedUseSsl) && parsedUseSsl;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Email settings are incomplete. Skipping email send to {Email}.", to);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = "Email settings are incomplete."
                };
            }

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(fromEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            try
            {
                var socketOptions = useSsl || port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                await smtp.ConnectAsync(host, port, socketOptions);
                await smtp.AuthenticateAsync(username, password);
                await smtp.SendAsync(email);
                return new EmailSendResult { Success = true };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Email sending failed to {Email}.", to);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
