using NotificationModel = Raphael.Notification.Domain.Models.Notification;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface INotificationRepository
{
    Task<NotificationModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationModel>> GetByRecipientAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationModel>> GetByBusinessEventCodeAsync(
        string businessEventCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationModel>> GetPendingNotificationsAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    //Task<int> SaveChangesAsync(
       // CancellationToken cancellationToken = default);
}