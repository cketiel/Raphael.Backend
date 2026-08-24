using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.DbContexts;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly RaphaelContext _context;


    public NotificationRepository(
        RaphaelContext context)
    {
        _context = context;
    }


    public async Task AddAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(
            notification,
            cancellationToken);
    }


    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }


    public async Task<NotificationModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.Recipients)
            .Include(n => n.Deliveries)
            .Include(n => n.Metadata)
            .Include(n => n.Actions)
            .FirstOrDefaultAsync(
                n => n.Id == id,
                cancellationToken);
    }


    public async Task<IReadOnlyList<NotificationModel>> GetByRecipientAsync(
        Guid recipientId,
        int recipientTypeId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Notifications
            .Include(n => n.Recipients)
            .Where(n => n.Recipients
                .Any(r => r.RecipientId == recipientId
                          && r.RecipientTypeId == recipientTypeId))
            .Where(n => n.ExpiresAtUtc == null
                        || n.ExpiresAtUtc > now)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}