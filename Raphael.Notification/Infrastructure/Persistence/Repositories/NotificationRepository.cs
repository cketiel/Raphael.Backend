using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;
using Raphael.Shared.DbContexts;

namespace Raphael.Notification.Infrastructure.Persistence.Repositories;

public class NotificationRepository //: INotificationRepository
{
    private readonly RaphaelContext _context;

    public NotificationRepository(RaphaelContext context)
    {
        _context = context;
    }

    /*public async Task<NotificationModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationModel>> GetByRecipientAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRecipients
            .Where(r => r.RecipientId == recipientId)
            .Join(
                _context.Notifications,
                recipient => recipient.NotificationId,
                notification => notification.Id,
                (_, notification) => notification)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationModel>> GetByBusinessEventCodeAsync(
        string businessEventCode,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(x => x.BusinessEventCode == businessEventCode)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationModel>> GetPendingNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(x => x.Status == Domain.Definitions.NotificationStatus.PendingDelivery)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(
            notification,
            cancellationToken);
    }

    public Task UpdateAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(notification);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default)
    {
        _context.Notifications.Remove(notification);

        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AnyAsync(
                x => x.Id == id,
                cancellationToken);
    }*/
}