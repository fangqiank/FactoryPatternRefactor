using FactoryPatternRefactor.Models;

namespace FactoryPatternRefactor.Interfaces
{
    public interface INotificationSenderFactory
    {
        INotificationSender GetSender(NotificationChannel channel);
    }
}
