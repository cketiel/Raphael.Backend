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

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}