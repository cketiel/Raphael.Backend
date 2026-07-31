using Raphael.Notification.Domain.Events.Payloads;
using Raphael.Notification.Domain.Rules;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;

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