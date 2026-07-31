using Raphael.Notification.Application.Delivery;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Infrastructure.Delivery;


public class EmailSender : INotificationSender
{
    public DeliveryChannel Channel
    => DeliveryChannel.Email;
    public Task<NotificationSenderResult> SendAsync(
        NotificationModel notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            NotificationSenderResult.Ok(
                "Email notification sent"));
    }
}