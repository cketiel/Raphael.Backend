using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Shared.Entities.Notifications;

public class NotificationRecipient
{
    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }

    public Guid RecipientId { get; private set; }

    public RecipientType RecipientType { get; private set; }

    public NotificationStatus Status { get; private set; }

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
        RecipientType = recipientType;

        Id = Guid.NewGuid();
        Status = NotificationStatus.Created;
    }

    public void MarkDelivered()
    {
        Status = NotificationStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
    }

    public void MarkViewed()
    {
        Status = NotificationStatus.Viewed;
        ViewedAtUtc = DateTime.UtcNow;
    }

    public void Acknowledge()
    {
        Status = NotificationStatus.Acknowledged;
        AcknowledgedAtUtc = DateTime.UtcNow;
    }
}