using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface INotificationRuleRepository
{
    Task<NotificationRule?> GetActiveRuleAsync(
        string businessEventCode,
        CancellationToken cancellationToken = default);
}