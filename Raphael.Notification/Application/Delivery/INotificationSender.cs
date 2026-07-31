using Raphael.Notification.Domain.Definitions;
using Raphael.Notification.Domain.Models;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;
namespace Raphael.Notification.Application.Delivery;

public interface INotificationSender
{
    DeliveryChannel Channel { get; }
    Task<NotificationSenderResult> SendAsync(
        NotificationModel notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default);
}