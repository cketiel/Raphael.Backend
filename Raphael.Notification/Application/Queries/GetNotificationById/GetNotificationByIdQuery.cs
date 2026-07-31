namespace Raphael.Notification.Application.Queries.GetNotificationById;

public class GetNotificationByIdQuery
{
    public Guid NotificationId { get; }


    public GetNotificationByIdQuery(
        Guid notificationId)
    {
        NotificationId = notificationId;
    }
}