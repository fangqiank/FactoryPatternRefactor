using System.Text;
using System.Text.Json;
using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;
using Microsoft.Extensions.Options;

namespace FactoryPatternRefactor.Senders
{
    public class TeamsNotificationSender : INotificationSender
    {
        private readonly ILogger<TeamsNotificationSender> _logger;
        private readonly TeamsSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public TeamsNotificationSender(
            IOptions<TeamsSettings> options,
            ILogger<TeamsNotificationSender> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public NotificationChannel Channel => NotificationChannel.Teams;

        public async Task SendAsync(NotificationMessage message)
        {
            // Teams Incoming Webhook 使用 Adaptive Card
            var card = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.0",
                            body = BuildBody(message)
                        }
                    }
                }
            };

            var payload = JsonSerializer.Serialize(card);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _settings.WebhookUrl);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("[Teams] Failed to send to {Recipient}: {StatusCode} - {Error}",
                    message.Recipient, response.StatusCode, error);
                throw new InvalidOperationException($"Teams send failed: {response.StatusCode}");
            }

            _logger.LogInformation("[Teams] Successfully sent to {Recipient}", message.Recipient);
        }

        private static List<object> BuildBody(NotificationMessage message)
        {
            var body = new List<object>
            {
                new
                {
                    type = "TextBlock",
                    text = string.IsNullOrEmpty(message.Subject) ? message.Body : message.Subject,
                    weight = "Bolder",
                    size = "Medium"
                }
            };

            if (!string.IsNullOrEmpty(message.Subject) && !string.IsNullOrEmpty(message.Body))
            {
                body.Add(new
                {
                    type = "TextBlock",
                    text = message.Body,
                    wrap = true
                });
            }

            return body;
        }
    }
}
