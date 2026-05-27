using System.Text;
using System.Text.Json;
using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;
using Microsoft.Extensions.Options;

namespace FactoryPatternRefactor.Senders
{
    public class SlackNotificationSender : INotificationSender
    {
        private readonly ILogger<SlackNotificationSender> _logger;
        private readonly SlackSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public SlackNotificationSender(
            IOptions<SlackSettings> options,
            ILogger<SlackNotificationSender> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public NotificationChannel Channel => NotificationChannel.Slack;

        public async Task SendAsync(NotificationMessage message)
        {
            // Slack Incoming Webhook 已绑定 channel，payload 只需 text
            var text = string.IsNullOrEmpty(message.Subject)
                ? message.Body
                : $"*{message.Subject}*\n{message.Body}";

            var payload = JsonSerializer.Serialize(new { text });

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _settings.WebhookUrl);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("[Slack] Failed to send to {Recipient}: {StatusCode} - {Error}",
                    message.Recipient, response.StatusCode, error);
                throw new InvalidOperationException($"Slack send failed: {response.StatusCode}");
            }

            _logger.LogInformation("[Slack] Successfully sent to {Recipient}", message.Recipient);
        }
    }
}
