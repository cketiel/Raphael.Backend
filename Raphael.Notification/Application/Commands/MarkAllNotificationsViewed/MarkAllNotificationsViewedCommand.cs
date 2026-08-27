using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Commands.MarkAllNotificationsViewed;

public class MarkAllNotificationsViewedCommand
{
    public Guid RecipientId { get; }

    public RecipientType RecipientType { get; }


    public MarkAllNotificationsViewedCommand(
        Guid recipientId,
        RecipientType recipientType)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        RecipientId = recipientId;
        RecipientType = recipientType;
    }
}
