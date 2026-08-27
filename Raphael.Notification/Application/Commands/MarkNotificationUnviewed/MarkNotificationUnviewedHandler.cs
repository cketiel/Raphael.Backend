using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;

namespace Raphael.Notification.Application.Commands.MarkNotificationUnviewed;

/// <summary>
/// Puts one notification back in the unread pile.
/// </summary>
/// <remarks>
/// The mirror image of <c>MarkNotificationViewedHandler</c>. Somebody who opens a notice while
/// driving and cannot act on it right then needs a way to leave it standing; without this the
/// only way back is to remember it.
/// </remarks>
public sealed class MarkNotificationUnviewedHandler
{
    private readonly INotificationRecipientRepository _recipientRepository;
    private readonly INotificationDispatcher _dispatcher;

    public MarkNotificationUnviewedHandler(
        INotificationRecipientRepository recipientRepository,
        INotificationDispatcher dispatcher)
    {
        _recipientRepository = recipientRepository;
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        MarkNotificationUnviewedCommand command,
        CancellationToken cancellationToken = default)
    {
        var recipient =
            await _recipientRepository.GetByIdAsync(
                command.NotificationRecipientId,
                cancellationToken);

        if (recipient == null)
            return;

        recipient.MarkUnviewed();

        await _recipientRepository.SaveChangesAsync(
            cancellationToken);

        // Every device of the same user recalculates its badge. No NotifyViewed counterpart
        // exists on the client contract, so the refresh is what carries the change across.
        await _dispatcher.RefreshNotificationsAsync(
            recipient.RecipientId,
            recipient.RecipientType,
            cancellationToken);
    }
}
