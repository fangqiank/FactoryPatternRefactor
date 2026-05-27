namespace FactoryPatternRefactor.Models
{
    public record NotificationMessage(
        string Recipient,
        string Subject,
        string Body,
        Dictionary<string, string>? Metadata = null);

    public record NotificationRequest(
        NotificationChannel Channel,
        string Recipient,
        string Subject,
        string Body);

    public enum NotificationChannel
    {
        Email,
        SMS,
        Slack,
        Teams
    }
}
