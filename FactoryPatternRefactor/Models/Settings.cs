namespace FactoryPatternRefactor.Models
{
    public class SmtpSettings
    {
        public string Server { get; set; } = "smtp.example.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "demo";
        public string Password { get; set; } = "demo";
        public string FromAddress { get; set; } = "noreply@example.com";
    }

    public class SmsSettings
    {
        public string Provider { get; set; } = "Twilio";
        public string AccountSid { get; set; } = "";
        public string AuthToken { get; set; } = "";
        public string FromNumber { get; set; } = "";
    }

    public class SlackSettings
    {
        public string WebhookUrl { get; set; } = "";
        public string DefaultChannel { get; set; } = "#general";
    }
}
