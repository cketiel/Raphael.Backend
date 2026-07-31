using Raphael.Notification.Domain.Definitions;

namespace Raphael.Notification.Domain.Models;

public class NotificationDelivery
{
    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }

    public DeliveryChannel Channel { get; private set; }

    public NotificationStatus Status { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public string? ExternalMessageId { get; private set; }

    public string? FailureReason { get; private set; }

    private NotificationDelivery()
    {
        // Required by EF Core
    }

    public NotificationDelivery(
        Guid notificationId,
        DeliveryChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        Id = Guid.NewGuid();
        NotificationId = notificationId;
        Channel = channel;
        Status = NotificationStatus.PendingDelivery;
    }

    public void MarkDelivered(string? externalMessageId = null)
    {
        Status = NotificationStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
        ExternalMessageId = externalMessageId;
        FailureReason = null;
    }

    public void MarkFailed(string failureReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        Status = NotificationStatus.Cancelled;
        FailureReason = failureReason;
    }
}