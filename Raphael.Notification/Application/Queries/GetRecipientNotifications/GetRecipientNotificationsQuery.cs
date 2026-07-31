namespace Raphael.Notification.Application.Queries.GetRecipientNotifications;

public class GetRecipientNotificationsQuery
{
    public Guid RecipientId { get; }


    public GetRecipientNotificationsQuery(
        Guid recipientId)
    {
        RecipientId = recipientId;
    }
}