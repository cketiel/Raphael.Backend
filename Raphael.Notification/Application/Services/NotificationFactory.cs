using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Factories;
using Raphael.Shared.Entities.Notifications;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationFactory
    : INotificationFactory
{
    public NotificationModel Create(
        NotificationRule rule,
        BusinessEventContext context)
    {
        ArgumentNullException.ThrowIfNull(rule);

        ArgumentNullException.ThrowIfNull(context);


        var notification = new NotificationModel(
            businessEventCode:
                rule.BusinessEventDefinition.Code,

            priority:
                rule.Priority,

            severity:
                rule.Severity,

            type:
                rule.NotificationType,

            title:
                rule.Name,

            message:
                rule.Description
        );


        foreach (var recipient in rule.Recipients)
        {
            notification.Recipients.Add(
                new NotificationRecipient(
                    notification.Id,
                    recipient.Id,
                    recipient.RecipientType));
        }


        foreach (var action in rule.Actions)
        {
            notification.Actions.Add(
                new NotificationAction(
                    notification.Id,
                    action.ActionCode,
                    action.Order,
                    false));
        }


        return notification;
    }
}