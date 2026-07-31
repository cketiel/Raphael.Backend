using Raphael.Notification.Application.DTOs;

namespace Raphael.Notification.Application.Interfaces.Realtime;

public interface INotificationDispatcher
{
    Task SendNotificationAsync(
        Guid userId,
        NotificationDto notification,
        CancellationToken cancellationToken = default);

    Task RefreshNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task NotifyViewedAsync(
     Guid userId,
     Guid notificationRecipientId,
     CancellationToken cancellationToken = default);

    Task NotifyAcknowledgedAsync(
        Guid userId,
        Guid notificationRecipientId,
        CancellationToken cancellationToken = default);
}