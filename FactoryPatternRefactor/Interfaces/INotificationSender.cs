using FactoryPatternRefactor.Models;

namespace FactoryPatternRefactor.Interfaces
{
    public interface INotificationSender
    {
        Task SendAsync(NotificationMessage message);
        NotificationChannel Channel { get; }
    }
}
