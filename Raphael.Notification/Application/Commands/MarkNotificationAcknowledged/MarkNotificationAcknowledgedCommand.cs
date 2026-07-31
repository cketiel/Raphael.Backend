namespace Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;

public class MarkNotificationAcknowledgedCommand
{
    public Guid NotificationRecipientId { get; }


    public MarkNotificationAcknowledgedCommand(
        Guid notificationRecipientId)
    {
        NotificationRecipientId = notificationRecipientId;
    }
}