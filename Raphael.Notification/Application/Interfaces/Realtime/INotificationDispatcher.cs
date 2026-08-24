using Raphael.Notification.Application.DTOs;
using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Interfaces.Realtime;

/// <summary>
/// Real time delivery of notifications.
/// </summary>
/// <remarks>
/// Every method takes the recipient type alongside the identifier. Without it the
/// destination cannot be resolved: the identifier of desktop user 5 and of patient 5
/// only differ by the type marker, and each one is reached through a different channel.
/// </remarks>
public interface INotificationDispatcher
{
    Task SendNotificationAsync(
        Guid recipientId,
        RecipientType recipientType,
        NotificationDto notification,
        CancellationToken cancellationToken = default);

    Task RefreshNotificationsAsync(
        Guid recipientId,
        RecipientType recipientType,
        CancellationToken cancellationToken = default);

    Task NotifyViewedAsync(
        Guid recipientId,
        RecipientType recipientType,
        Guid notificationRecipientId,
        CancellationToken cancellationToken = default);

    Task NotifyAcknowledgedAsync(
        Guid recipientId,
        RecipientType recipientType,
        Guid notificationRecipientId,
        CancellationToken cancellationToken = default);
}
