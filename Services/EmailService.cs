using System.Net;
using System.Net.Mail;

namespace MoodBite.Services
{
    public interface IEmailService
    {
        bool IsConfigured { get; }
        Task<EmailSendResult> SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default);
        Task<EmailSendResult> SendClinicInvitationAsync(string toEmail, string clinicName, string invitationUrl, CancellationToken cancellationToken = default);
    }

    public sealed record EmailSendResult(bool Sent, string? DevelopmentPreviewUrl = null);

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public bool IsConfigured => GetSmtpSettings().IsConfigured;

        public Task<EmailSendResult> SendPasswordResetAsync(
            string toEmail,
            string resetUrl,
            CancellationToken cancellationToken = default) =>
            SendAsync(
                toEmail,
                "Reset your MoodBite password",
                $"Use this link to reset your MoodBite password: {resetUrl}",
                resetUrl,
                "password reset",
                cancellationToken);

        public Task<EmailSendResult> SendClinicInvitationAsync(
            string toEmail,
            string clinicName,
            string invitationUrl,
            CancellationToken cancellationToken = default) =>
            SendAsync(
                toEmail,
                $"Invitation to join {clinicName} on MoodBite",
                $"{clinicName} invited you to connect on MoodBite. Use this link to accept the invitation: {invitationUrl}",
                invitationUrl,
                "clinic invitation",
                cancellationToken);

        private async Task<EmailSendResult> SendAsync(
            string toEmail,
            string subject,
            string body,
            string developmentPreviewUrl,
            string purpose,
            CancellationToken cancellationToken)
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("Development email preview generated for {Purpose} to {RecipientHash}.", purpose, HashForLog(toEmail));
                return new EmailSendResult(false, developmentPreviewUrl);
            }

            var settings = GetSmtpSettings();
            if (!settings.IsConfigured)
            {
                _logger.LogWarning("Email provider is not configured. {Purpose} email was not sent to {RecipientHash}.", purpose, HashForLog(toEmail));
                return new EmailSendResult(false);
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(settings.FromEmail!, settings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                message.To.Add(toEmail);

                using var client = new SmtpClient(settings.Host!, settings.Port)
                {
                    EnableSsl = settings.EnableSsl
                };

                if (!string.IsNullOrWhiteSpace(settings.Username))
                {
                    client.Credentials = new NetworkCredential(settings.Username, settings.Password);
                }

                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("Email sent for {Purpose} to {RecipientHash}.", purpose, HashForLog(toEmail));
                return new EmailSendResult(true);
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or IOException)
            {
                _logger.LogWarning(ex, "Email delivery failed for {Purpose} to {RecipientHash}.", purpose, HashForLog(toEmail));
                return new EmailSendResult(false);
            }
        }

        private SmtpSettings GetSmtpSettings()
        {
            var provider = _configuration["Email:Provider"];
            var host = _configuration["Email:Smtp:Host"];
            var fromEmail = _configuration["Email:FromEmail"];
            var port = GetInt("Email:Smtp:Port", 587);
            var enableSsl = GetBool("Email:Smtp:EnableSsl", true);

            return new SmtpSettings(
                provider,
                host,
                port,
                _configuration["Email:Smtp:Username"],
                _configuration["Email:Smtp:Password"],
                fromEmail,
                _configuration["Email:FromName"] ?? "MoodBite",
                enableSsl);
        }

        private int GetInt(string key, int fallback) =>
            int.TryParse(_configuration[key], out var value) && value > 0 ? value : fallback;

        private bool GetBool(string key, bool fallback) =>
            bool.TryParse(_configuration[key], out var value) ? value : fallback;

        private static string HashForLog(string value)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
            return Convert.ToHexString(bytes)[..12];
        }

        private sealed record SmtpSettings(
            string? Provider,
            string? Host,
            int Port,
            string? Username,
            string? Password,
            string? FromEmail,
            string FromName,
            bool EnableSsl)
        {
            public bool IsConfigured =>
                string.Equals(Provider, "Smtp", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(Host) &&
                !string.IsNullOrWhiteSpace(FromEmail);
        }
    }
}
