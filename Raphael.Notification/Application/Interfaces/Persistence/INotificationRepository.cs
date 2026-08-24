using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface INotificationRepository
{
    Task AddAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default);


    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);


    Task<NotificationModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Inbox of one recipient. The recipient type is part of the filter on purpose:
    /// the identifier alone does not tell a patient apart from a dispatcher.
    /// Expired notifications are left out.
    /// </summary>
    Task<IReadOnlyList<NotificationModel>> GetByRecipientAsync(
        Guid recipientId,
        int recipientTypeId,
        CancellationToken cancellationToken = default);
}