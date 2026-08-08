using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Infrastructure.Persistence;

public class NotificationDeliveryRepository
    : INotificationDeliveryRepository
{
    private readonly RaphaelContext _context;


    public NotificationDeliveryRepository(
        RaphaelContext context)
    {
        _context = context;
    }


    public async Task<NotificationDelivery?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationDeliveries
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }


    public async Task UpdateAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        _context.NotificationDeliveries.Update(delivery);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    public async Task AddAsync(
    NotificationDelivery delivery,
    CancellationToken cancellationToken = default)
    {
        await _context.NotificationDeliveries.AddAsync(
            delivery,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

}