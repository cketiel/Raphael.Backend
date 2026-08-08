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