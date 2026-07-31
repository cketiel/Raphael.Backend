using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface INotificationRepository
{
    Task AddAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default);


    Task<NotificationModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);


    Task<IReadOnlyList<NotificationModel>> GetByRecipientAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);
}