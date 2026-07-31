using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Shared.Entities.Notifications;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Interfaces.Factories;

public interface INotificationFactory
{
    NotificationModel Create(
        NotificationRule rule,
        BusinessEventContext context);
}