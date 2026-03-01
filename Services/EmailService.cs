using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net;
using System.IO;
using TechNova_IT_Solutions.Services.Interfaces;
using System.Threading.Tasks;

namespace TechNova_IT_Solutions.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var host = emailSettings["Host"];
            var port = int.Parse(emailSettings["Port"] ?? "587");
            var username = emailSettings["Username"]?.Trim();
            var password = emailSettings["Password"]?.Replace(" ", string.Empty).Trim();
            var fromEmail = (emailSettings["FromEmail"] ?? username)?.Trim();
            var useSsl = bool.TryParse(emailSettings["UseSsl"], out var parsedUseSsl) && parsedUseSsl;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogWarning("Email settings are incomplete. Skipping email send to {Email}.", to);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = "Email settings are incomplete."
                };
            }

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(fromEmail!));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            var htmlBody = BuildBrandedHtmlBody(subject, body, fromEmail);
            var htmlPart = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };
            var logoPart = TryCreateInlineLogoPart();

            if (logoPart != null)
            {
                var related = new Multipart("related");
                related.Add(htmlPart);
                related.Add(logoPart);
                email.Body = related;
            }
            else
            {
                email.Body = htmlPart;
            }

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

        private static string BuildBrandedHtmlBody(string subject, string body, string? fromEmail)
        {
            if (!string.IsNullOrWhiteSpace(body) &&
                body.Contains("<html", StringComparison.OrdinalIgnoreCase))
            {
                return body;
            }

            var safeSubject = WebUtility.HtmlEncode(subject ?? "TechNova Notification");
            var safeFrom = WebUtility.HtmlEncode(fromEmail ?? "TechNova");
            var content = string.IsNullOrWhiteSpace(body)
                ? "<p style=\"margin:0;color:#1f2937;\">No message content provided.</p>"
                : body;

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{safeSubject}</title>
</head>
<body style=""margin:0;padding:0;background:#f4f7fb;font-family:Segoe UI,Arial,sans-serif;color:#111827;"">
  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background:#f4f7fb;padding:24px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""620"" style=""max-width:620px;width:100%;background:#ffffff;border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;"">
          <tr>
            <td style=""padding:20px 24px;background:linear-gradient(90deg,#0f172a,#1d4ed8);"">
              <div style=""margin-bottom:12px;"">
                <img src=""cid:technova-logo"" alt=""TechNova logo"" width=""56"" style=""display:block;border:0;outline:none;text-decoration:none;width:56px;height:auto;"" />
              </div>
              <div style=""font-size:12px;letter-spacing:.08em;text-transform:uppercase;color:#bfdbfe;"">TechNova IT Solutions</div>
              <div style=""margin-top:6px;font-size:22px;font-weight:700;color:#ffffff;"">{safeSubject}</div>
            </td>
          </tr>
          <tr>
            <td style=""padding:24px;font-size:15px;line-height:1.65;color:#1f2937;"">
              {content}
            </td>
          </tr>
          <tr>
            <td style=""padding:16px 24px;border-top:1px solid #e5e7eb;background:#f8fafc;color:#6b7280;font-size:12px;line-height:1.6;"">
              Sent by {safeFrom}. If this was not expected, contact your administrator.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        private MimePart? TryCreateInlineLogoPart()
        {
            try
            {
                var webRoot = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    return null;
                }

                var logoPath = Path.Combine(webRoot, "images", "logo2.png");
                if (!File.Exists(logoPath))
                {
                    return null;
                }

                var logo = new MimePart("image", "png")
                {
                    Content = new MimeContent(File.OpenRead(logoPath)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    ContentId = "technova-logo"
                };

                return logo;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Unable to attach inline email logo.");
                return null;
            }
        }
    }
}
