using System.Net.Http.Headers;
using System.Text;
using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;
using Microsoft.Extensions.Options;

namespace FactoryPatternRefactor.Senders
{
    public class SmsNotificationSender : INotificationSender
    {
        private readonly ILogger<SmsNotificationSender> _logger;
        private readonly SmsSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public SmsNotificationSender(
            IOptions<SmsSettings> options,
            ILogger<SmsNotificationSender> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public NotificationChannel Channel => NotificationChannel.SMS;

        public async Task SendAsync(NotificationMessage message)
        {
            var client = _httpClientFactory.CreateClient();

            var authBytes = Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}");
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = message.Recipient,
                ["From"] = _settings.FromNumber,
                ["Body"] = string.IsNullOrEmpty(message.Subject) ? message.Body : $"{message.Subject}: {message.Body}"
            });

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("[SMS] Failed to send to {Recipient}: {StatusCode} - {Error}",
                    message.Recipient, response.StatusCode, error);
                throw new InvalidOperationException($"SMS send failed: {response.StatusCode}");
            }

            _logger.LogInformation("[SMS] Successfully sent to {Recipient}", message.Recipient);
        }
    }
}
