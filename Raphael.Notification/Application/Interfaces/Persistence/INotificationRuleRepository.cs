using Raphael.Notification.Domain.Rules;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface INotificationRuleRepository
{
    Task<NotificationRule?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<NotificationRule?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationRule>> GetByBusinessEventCodeAsync(
        string businessEventCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationRule>> GetActiveRulesAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        NotificationRule rule,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        NotificationRule rule,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        NotificationRule rule,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    //Task<int> SaveChangesAsync(
        //CancellationToken cancellationToken = default);
}