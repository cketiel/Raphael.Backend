using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.DbContexts;
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
}