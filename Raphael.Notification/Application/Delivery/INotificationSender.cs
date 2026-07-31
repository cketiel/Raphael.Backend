using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;
namespace Raphael.Notification.Application.Delivery;

public interface INotificationSender
{
    DeliveryChannel Channel { get; }
    Task<NotificationSenderResult> SendAsync(
        NotificationModel notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default);
}