using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Commands.DeleteSignal;

public class DeleteSignalCommand
{
    /// <summary>Recipient row the application consumed.</summary>
    public Guid NotificationRecipientId { get; }

    /// <summary>Who is asking, so a signal can only be deleted by its own recipient.</summary>
    public Guid RecipientId { get; }

    public RecipientType RecipientType { get; }


    public DeleteSignalCommand(
        Guid notificationRecipientId,
        Guid recipientId,
        RecipientType recipientType)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        NotificationRecipientId = notificationRecipientId;
        RecipientId = recipientId;
        RecipientType = recipientType;
    }
}
