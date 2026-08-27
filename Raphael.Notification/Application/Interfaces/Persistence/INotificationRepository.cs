using Raphael.Notification.Application.Queries.GetRecipientNotifications;
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
    /// <param name="scope">
    /// Notices, what a person reads, or signals, what an application acts on. They never
    /// come back together: a signal in an inbox is a row nobody can do anything with.
    /// </param>
    Task<IReadOnlyList<NotificationModel>> GetByRecipientAsync(
        Guid recipientId,
        int recipientTypeId,
        NotificationScope scope = NotificationScope.Notices,
        CancellationToken cancellationToken = default);
}