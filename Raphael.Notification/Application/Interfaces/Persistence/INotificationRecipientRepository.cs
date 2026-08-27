using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface INotificationRecipientRepository
{
    Task<NotificationRecipient?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);


    Task UpdateAsync(
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The rows this recipient has not read yet, out of the notifications still visible.
    /// </summary>
    /// <remarks>
    /// Expired notifications are left out on purpose: they no longer appear in the inbox, so
    /// counting them would show a badge over a list with nothing in it.
    /// </remarks>
    Task<IReadOnlyList<NotificationRecipient>> GetUnviewedAsync(
        Guid recipientId,
        int recipientTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How many unread notifications this recipient has. Same window as
    /// <see cref="GetUnviewedAsync"/>, so the badge and the list can never disagree.
    /// </summary>
    Task<int> CountUnviewedAsync(
        Guid recipientId,
        int recipientTypeId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}