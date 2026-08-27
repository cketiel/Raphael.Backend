namespace Raphael.Notification.Application.Commands.MarkNotificationUnviewed;

public class MarkNotificationUnviewedCommand
{
    public Guid NotificationRecipientId { get; }


    public MarkNotificationUnviewedCommand(
        Guid notificationRecipientId)
    {
        NotificationRecipientId = notificationRecipientId;
    }
}
