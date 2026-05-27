using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace FactoryPatternRefactor.Senders
{
    public class EmailNotificationSender(
        IOptions<SmtpSettings> options,
        ILogger<EmailNotificationSender> logger)
        : INotificationSender
    {
        public NotificationChannel Channel => NotificationChannel.Email;

        private readonly SmtpSettings _settings = options.Value;

        public async Task SendAsync(NotificationMessage message)
        {
            try
            {
                using var smtp = new SmtpClient(_settings.Server, _settings.Port);
                smtp.EnableSsl = true;
                smtp.Credentials = new System.Net.NetworkCredential(_settings.Username, _settings.Password);

                logger.LogInformation(
                    "[Email] Sending to {Recipient} via {Server}:{Port} - Subject: {Subject}",
                    message.Recipient,
                    _settings.Server,
                    _settings.Port,
                    message.Subject);

                var mailMessage = new MailMessage(
                    from: _settings.FromAddress,
                    to: message.Recipient,
                    subject: message.Subject,
                    message.Body);
                mailMessage.IsBodyHtml = true;

                await smtp.SendMailAsync(mailMessage);

                logger.LogInformation(
                    "[Email] Successfully sent to {Recipient}",
                    message.Recipient);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Email] Failed to send to {Recipient} via {Server}:{Port}",
                    message.Recipient, _settings.Server, _settings.Port);
                throw new InvalidOperationException($"Email send failed: {ex.Message}", ex);
            }
        }
    }
}
