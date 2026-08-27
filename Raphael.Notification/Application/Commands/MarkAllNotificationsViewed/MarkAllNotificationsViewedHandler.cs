using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;

namespace Raphael.Notification.Application.Commands.MarkAllNotificationsViewed;

/// <summary>
/// Clears a recipient's whole unread pile in one go. Returns how many rows it reached.
/// </summary>
/// <remarks>
/// One save and one refresh for the entire batch, rather than looping over the single row
/// handler: a driver coming back from a shift with a dozen unread notices would otherwise
/// fire a dozen round trips and a dozen badge updates for one tap.
/// </remarks>
public sealed class MarkAllNotificationsViewedHandler
{
    private readonly INotificationRecipientRepository _recipientRepository;
    private readonly INotificationDispatcher _dispatcher;

    public MarkAllNotificationsViewedHandler(
        INotificationRecipientRepository recipientRepository,
        INotificationDispatcher dispatcher)
    {
        _recipientRepository = recipientRepository;
        _dispatcher = dispatcher;
    }

    public async Task<int> Handle(
        MarkAllNotificationsViewedCommand command,
        CancellationToken cancellationToken = default)
    {
        var unviewed =
            await _recipientRepository.GetUnviewedAsync(
                command.RecipientId,
                command.RecipientType.Id,
                cancellationToken);

        if (unviewed.Count == 0)
            return 0;

        foreach (var recipient in unviewed)
        {
            recipient.MarkViewed();
        }

        await _recipientRepository.SaveChangesAsync(
            cancellationToken);

        await _dispatcher.RefreshNotificationsAsync(
            command.RecipientId,
            command.RecipientType,
            cancellationToken);

        return unviewed.Count;
    }
}
