namespace FactoryPatternRefactor.Models
{
    public record SendNotificationsResult(
        int Total,
        int Succeeded,
        int Failed,
        List<SendResultDetail> Details);

    public record SendResultDetail(
        NotificationChannel Channel,
        string Recipient,
        bool Success,
        string? Error);
}
