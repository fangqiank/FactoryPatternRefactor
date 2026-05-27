using System.Text;
using System.Text.Json;
using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;
using Microsoft.Extensions.Options;

namespace FactoryPatternRefactor.Senders
{
    public class SlackNotificationSender(
        IOptions<SlackSettings> options,
        ILogger<SlackNotificationSender> logger,
        IHttpClientFactory httpClientFactory)
        : INotificationSender
    {
        private readonly SlackSettings _settings = options.Value;

        public NotificationChannel Channel => NotificationChannel.Slack;

        public async Task SendAsync(NotificationMessage message)
        {
            // Slack Incoming Webhook 已绑定 channel，payload 只需 text
            var text = string.IsNullOrEmpty(message.Subject)
                ? message.Body
                : $"*{message.Subject}*\n{message.Body}";

            var payload = JsonSerializer.Serialize(new { text });

            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _settings.WebhookUrl);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("[Slack] Failed to send to {Recipient}: {StatusCode} - {Error}",
                    message.Recipient, response.StatusCode, error);
                throw new InvalidOperationException($"Slack send failed: {response.StatusCode}");
            }

            logger.LogInformation("[Slack] Successfully sent to {Recipient}", message.Recipient);
        }
    }
}
