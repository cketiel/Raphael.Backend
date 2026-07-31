using Raphael.Shared.Entities.Notifications;
using Raphael.Shared.Entities.Notifications.Payloads;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Domain.Factories;

public class NotificationFactory
{
    public NotificationModel Create(
        NotificationRule rule,
        NotificationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(rule);

        ArgumentNullException.ThrowIfNull(payload);


        return new NotificationModel(
            payload.EventCode,
            rule.Priority,
            rule.Severity,
            rule.NotificationType,
            rule.Name,
            rule.Description);
    }
}