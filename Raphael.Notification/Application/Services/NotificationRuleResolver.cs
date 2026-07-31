using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Rules;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationRuleResolver
    : INotificationRuleResolver
{
    private readonly INotificationRuleRepository _repository;

    public NotificationRuleResolver(
        INotificationRuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<NotificationRule>> ResolveAsync(
        BusinessEventContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await _repository
            .GetActiveByBusinessEventCodeAsync(
                context.EventCode,
                cancellationToken);
    }
}