using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Infrastructure.Persistence;

public class NotificationRecipientRepository
    : INotificationRecipientRepository
{
    private readonly RaphaelContext _context;


    public NotificationRecipientRepository(
        RaphaelContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
       NotificationRecipient recipient,
       CancellationToken cancellationToken = default)
    {
        await _context.NotificationRecipients.AddAsync(
            recipient,
            cancellationToken);
    }


    public async Task<NotificationRecipient?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRecipients
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }


    public async Task UpdateAsync(
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default)
    {
        _context.NotificationRecipients.Update(recipient);

        await _context.SaveChangesAsync(
            cancellationToken);
    }


    public async Task<IReadOnlyList<NotificationRecipient>> GetUnviewedAsync(
        Guid recipientId,
        int recipientTypeId,
        CancellationToken cancellationToken = default)
    {
        return await Unviewed(recipientId, recipientTypeId)
            .ToListAsync(cancellationToken);
    }


    public async Task<int> CountUnviewedAsync(
        Guid recipientId,
        int recipientTypeId,
        CancellationToken cancellationToken = default)
    {
        return await Unviewed(recipientId, recipientTypeId)
            .CountAsync(cancellationToken);
    }


    /// <summary>
    /// Written once so the count and the list cannot drift apart: the same visibility window
    /// as <c>NotificationRepository.GetByRecipientAsync</c>, which is what feeds the inbox.
    /// </summary>
    private IQueryable<NotificationRecipient> Unviewed(
        Guid recipientId,
        int recipientTypeId)
    {
        var now = DateTime.UtcNow;

        return _context.NotificationRecipients
            .Where(x => x.RecipientId == recipientId
                        && x.RecipientTypeId == recipientTypeId
                        && x.ViewedAtUtc == null)
            .Where(x => x.Notification.ExpiresAtUtc == null
                        || x.Notification.ExpiresAtUtc > now)
            // ⚠️ Signals never count. Nobody reads one, so a badge including them would be a
            // number the driver has no way to clear.
            .Where(x => !x.Notification.Metadata
                .Any(m => m.Key == NotificationMetadataKeys.Signal));
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}