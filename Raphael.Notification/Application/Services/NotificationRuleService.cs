using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Services;

public class NotificationRuleService
{
    private readonly INotificationRuleRepository _notificationRuleRepository;


    public NotificationRuleService(
        INotificationRuleRepository notificationRuleRepository)
    {
        _notificationRuleRepository = notificationRuleRepository;
    }


    public async Task<NotificationRule?> GetActiveRuleAsync(
        string businessEventCode,
        CancellationToken cancellationToken = default)
    {
        return await _notificationRuleRepository.GetActiveRuleAsync(
            businessEventCode,
            cancellationToken);
    }
}