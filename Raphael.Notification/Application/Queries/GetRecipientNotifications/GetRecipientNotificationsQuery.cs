using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Queries.GetRecipientNotifications;

public class GetRecipientNotificationsQuery
{
    public Guid RecipientId { get; }

    /// <summary>
    /// Type of the recipient asking for its inbox. Required: the identifier on its own
    /// does not tell a patient apart from a dispatcher or an integration.
    /// </summary>
    public RecipientType RecipientType { get; }


    public GetRecipientNotificationsQuery(
        Guid recipientId,
        RecipientType recipientType)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        RecipientId = recipientId;
        RecipientType = recipientType;
    }
}
