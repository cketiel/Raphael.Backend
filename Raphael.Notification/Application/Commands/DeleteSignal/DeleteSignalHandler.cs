using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Commands.DeleteSignal;

/// <summary>
/// Deletes a signal once the application has acted on it.
/// </summary>
/// <remarks>
/// This is the one place in the notification module that removes a live row, and it is
/// allowed for one reason: a signal is not a record. It says "your schedule is stale", the
/// app reloads, and after that it describes nothing. Nobody will ever be asked to account
/// for one.
///
/// <para>
/// ⚠️ That is exactly what is not true of a notice. A cancellation is something somebody may
/// have to answer for months later, which is why a driver only ever hides those on their own
/// device and their deletion belongs to the retention policy alone. The guard below is what
/// keeps the two apart: anything without the signal marker is refused outright, so this
/// endpoint can never be turned into a way to erase a notice.
/// </para>
/// </remarks>
public sealed class DeleteSignalHandler
{
    private readonly RaphaelContext _context;

    public DeleteSignalHandler(RaphaelContext context)
    {
        _context = context;
    }

    /// <summary>
    /// True when a signal was found and deleted. False when there was nothing to delete,
    /// when the row belongs to somebody else, or when it is not a signal.
    /// </summary>
    public async Task<bool> Handle(
        DeleteSignalCommand command,
        CancellationToken cancellationToken = default)
    {
        var recipient = await _context.NotificationRecipients
            .Include(x => x.Notification)
                .ThenInclude(x => x.Metadata)
            .FirstOrDefaultAsync(
                x => x.Id == command.NotificationRecipientId,
                cancellationToken);

        if (recipient is null)
            return false;

        // Somebody else's row is reported as missing rather than as forbidden: confirming it
        // exists would already say more than the caller is entitled to know.
        if (recipient.RecipientId != command.RecipientId ||
            recipient.RecipientTypeId != command.RecipientType.Id)
        {
            return false;
        }

        var isSignal = recipient.Notification.Metadata
            .Any(m => m.Key == NotificationMetadataKeys.Signal);

        if (!isSignal)
            return false;

        // A signal has exactly one recipient, so removing the notification takes the whole
        // thing with it: recipients, metadata and deliveries all cascade.
        _context.Notifications.Remove(recipient.Notification);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
