using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Domain.Definitions;
using Raphael.Notification.Domain.Models;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;

namespace Raphael.Notification.Infrastructure.Delivery;


public class InAppSender : INotificationSender
{
    public DeliveryChannel Channel
        => DeliveryChannel.InApp;
    public Task<NotificationSenderResult> SendAsync(
        NotificationModel notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            NotificationSenderResult.Ok(
                "Notification stored for in-app delivery"));
    }
}