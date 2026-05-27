using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;

namespace FactoryPatternRefactor.Services
{
    // ❌ BEFORE: Messy NotificationService with too many responsibilities
    public class NotificationServiceBeforeRefactor
    {
        public Task SendNotifications(
            List<(NotificationChannel Channel,
            NotificationMessage Message)> notifications)
        {
            try
            {
                foreach (var (channel, message) in notifications)
                {
                    switch (channel)
                    {
                        case NotificationChannel.Email:
                            Console.WriteLine($"Email to {message.Recipient}: {message.Body}");
                            break;
                        case NotificationChannel.SMS:
                            Console.WriteLine($"SMS to {message.Recipient}: {message.Body}");
                            break;
                        case NotificationChannel.Slack:
                            Console.WriteLine($"Slack to {message.Recipient}: {message.Body}");
                            break;
                        case NotificationChannel.Teams:
                        default:
                            throw new ArgumentException($"Unknown channel: {channel}");
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }
    }

    // ✅ AFTER: Clean, focused NotificationService using factory
    public class NotificationService(
        INotificationSenderFactory senderFactory,
        ILogger<NotificationService> logger
        )
    {
        // Send single notification
        public async Task SendNotificationAsync(
            NotificationChannel channel,
            NotificationMessage message)
        {
            var sender = senderFactory.GetSender(channel);
            await sender.SendAsync(message);

            logger.LogInformation(
                "Notification sent via {Channel} to {Recipient}",
                channel,
                message.Recipient);
        }

        // Send multiple notifications with per-item failure tracking
        public async Task<SendNotificationsResult> SendNotificationsAsync(
            List<(NotificationChannel Channel,
            NotificationMessage Message)> notifications)
        {
            var results = new List<(NotificationChannel Channel, string Recipient, bool Success, string? Error)>();

            var tasks = notifications.Select(async n =>
            {
                try
                {
                    var sender = senderFactory.GetSender(n.Channel);
                    await sender.SendAsync(n.Message);
                    return (n.Channel, n.Message.Recipient, Success: true, Error: (string?)null);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send via {Channel} to {Recipient}",
                        n.Channel, n.Message.Recipient);
                    return (n.Channel, n.Message.Recipient, Success: false, Error: ex.Message);
                }
            });

            results = (await Task.WhenAll(tasks)).ToList();

            return new SendNotificationsResult(
                Total: results.Count,
                Succeeded: results.Count(r => r.Success),
                Failed: results.Count(r => !r.Success),
                Details: results.Select(r => new SendResultDetail(
                    r.Channel, r.Recipient, r.Success, r.Error)).ToList());
        }
    }
}
