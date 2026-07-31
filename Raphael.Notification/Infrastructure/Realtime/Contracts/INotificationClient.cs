using Raphael.Notification.Application.DTOs;

namespace Raphael.Notification.Infrastructure.Realtime.Contracts;

public interface INotificationClient
{
    Task ReceiveNotification(
        NotificationDto notification);

    Task NotificationViewed(
        Guid notificationId);

    Task NotificationAcknowledged(
        Guid notificationId);

    Task NotificationRemoved(
        Guid notificationId);

    Task RefreshNotifications();
}