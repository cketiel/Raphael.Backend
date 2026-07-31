using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Interfaces.Rules;

public interface INotificationRuleResolver
{
    Task<IReadOnlyCollection<NotificationRule>> ResolveAsync(
        BusinessEventContext context,
        CancellationToken cancellationToken = default);
}