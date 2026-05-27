using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;

namespace FactoryPatternRefactor.Factories
{
    // Approach 1: Dictionary-based factory (简单直接)
    public class DictionaryNotificationSenderFactory : INotificationSenderFactory
    {
        private readonly Dictionary<NotificationChannel, INotificationSender> _senders;

        public DictionaryNotificationSenderFactory(IEnumerable<INotificationSender> senders )
        {
            _senders = senders.ToDictionary(s => s.Channel);
        }
        public INotificationSender GetSender(NotificationChannel channel)
        {
            return _senders.TryGetValue(channel, out var sender) 
                ? sender 
                : throw new ArgumentException($"No sender registered for channel: {channel}");
        }
    }

    // Approach 2: IServiceProvider-based factory (更灵活，按需解析)
    public class ServiceProviderNotificationSenderFactory(IServiceProvider serviceProvider) : INotificationSenderFactory
    {
        //private readonly Dictionary<NotificationChannel, Type> _senderTypes = new()
        //{
        //    [NotificationChannel.Email] = typeof(EmailNotificationSender),
        //    [NotificationChannel.SMS] = typeof(SmsNotificationSender),
        //    [NotificationChannel.Slack] = typeof(SlackNotificationSender),
        //};

        public INotificationSender GetSender(NotificationChannel channel)
        {
            //if (!_senderTypes.TryGetValue(channel, out var senderType))
            //    throw new ArgumentException($"No sender registered for channel: {channel}");

            //return (INotificationSender)_serviceProvider.GetRequiredService(senderType);

            try
            {
                return serviceProvider.GetRequiredKeyedService<INotificationSender>(channel);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException($"No sender registered for channel: {channel}");
            }
        }
    }
}
