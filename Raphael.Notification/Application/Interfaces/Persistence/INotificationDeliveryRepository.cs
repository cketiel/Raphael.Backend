using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface INotificationDeliveryRepository
{
    Task<NotificationDelivery?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);


    Task UpdateAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default);

    Task AddAsync(
    NotificationDelivery delivery,
    CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}