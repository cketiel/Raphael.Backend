namespace Raphael.Notification.Application.Commands.MarkNotificationViewed;

public class MarkNotificationViewedCommand
{
    public Guid NotificationRecipientId { get; }


    public MarkNotificationViewedCommand(
        Guid notificationRecipientId)
    {
        NotificationRecipientId = notificationRecipientId;
    }
}