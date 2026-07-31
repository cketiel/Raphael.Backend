using Raphael.Notification.Domain.Definitions;

namespace Raphael.Notification.Application.Delivery;


public class NotificationSenderFactory
{
    private readonly IEnumerable<INotificationSender> _senders;


    public NotificationSenderFactory(
        IEnumerable<INotificationSender> senders)
    {
        _senders = senders;
    }



    public INotificationSender Create(
        DeliveryChannel channel)
    {
        var sender =
            _senders.FirstOrDefault(
                x => x.Channel == channel);



        if (sender == null)
        {
            throw new InvalidOperationException(
                $"No sender registered for channel {channel}");
        }


        return sender;
    }
}