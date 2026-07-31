using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Interfaces.Rules;

public interface INotificationRuleEvaluator
{
    Task<bool> EvaluateAsync(
        NotificationRule rule,
        BusinessEventContext context,
        CancellationToken cancellationToken = default);
}