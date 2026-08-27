using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities.Notifications;

public class NotificationRecipient
{
    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }
    public Notification Notification { get; private set; } = null!;

    public Guid RecipientId { get; private set; }

    public int RecipientTypeId { get; private set; }
    public int StatusId { get; private set; }
    [NotMapped]
    public RecipientType RecipientType
    => Enumeration.FromId<RecipientType>(RecipientTypeId);
    [NotMapped]
    public NotificationStatus Status
        => Enumeration.FromId<NotificationStatus>(StatusId);

    /*public RecipientType RecipientType { get; private set; }

    public NotificationStatus Status { get; private set; }*/

    public DateTime? DeliveredAtUtc { get; private set; }

    public DateTime? ViewedAtUtc { get; private set; }

    public DateTime? AcknowledgedAtUtc { get; private set; }

    private NotificationRecipient()
    {
        // Required by EF Core
    }

    public NotificationRecipient(
        Guid notificationId,
        Guid recipientId,
        RecipientType recipientType)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        NotificationId = notificationId;
        RecipientId = recipientId;

        RecipientTypeId = recipientType.Id;
        StatusId = NotificationStatus.Created.Id;

        //RecipientType = recipientType;
        //Status = NotificationStatus.Created;

        Id = Guid.NewGuid();      

    }

    public void MarkDelivered()
    {
        StatusId = NotificationStatus.Delivered.Id;
        //Status = NotificationStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
    }

    public void MarkViewed()
    {
        if (ViewedAtUtc.HasValue)
            return;

        ViewedAtUtc = DateTime.UtcNow;

        StatusId = NotificationStatus.Viewed.Id;
    }

    /// <summary>
    /// Puts the notification back in the unread pile.
    /// </summary>
    /// <remarks>
    /// ⚠️ Does nothing once the notification has been acknowledged. An acknowledgement is a
    /// promise made to somebody else — <c>WILL_CALL_ACKNOWLEDGED</c> tells a patient that the
    /// dispatch office took charge of their ride — and taking it back here would leave that
    /// promise standing with nobody behind it.
    ///
    /// <para>
    /// The status returns to <see cref="NotificationStatus.Delivered"/>, or to
    /// <see cref="NotificationStatus.Created"/> when the notification never reached the
    /// recipient in the first place. Unreading something is not the same as undelivering it.
    /// </para>
    /// </remarks>
    public void MarkUnviewed()
    {
        if (AcknowledgedAtUtc.HasValue)
            return;

        if (!ViewedAtUtc.HasValue)
            return;

        ViewedAtUtc = null;

        StatusId = DeliveredAtUtc.HasValue
            ? NotificationStatus.Delivered.Id
            : NotificationStatus.Created.Id;
    }

    public void MarkAcknowledged()
    {
        if (AcknowledgedAtUtc.HasValue)
            return;

        if (!ViewedAtUtc.HasValue)
        {
            ViewedAtUtc = DateTime.UtcNow;
        }

        AcknowledgedAtUtc = DateTime.UtcNow;

        StatusId = NotificationStatus.Acknowledged.Id;
    }
}