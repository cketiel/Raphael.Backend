using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Domain.Definitions;
using Raphael.Notification.Domain.Models;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;

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