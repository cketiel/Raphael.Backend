using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities.Notifications;

public class NotificationDelivery
{
    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }
    public Notification Notification { get; private set; } = null!;
    public int ChannelId { get; private set; }

    public int StatusId { get; private set; }
    [NotMapped]
    public DeliveryChannel Channel
    => Enumeration.FromId<DeliveryChannel>(ChannelId);
    [NotMapped]
    public NotificationStatus Status
        => Enumeration.FromId<NotificationStatus>(StatusId);

    /*public DeliveryChannel Channel { get; private set; }

    public NotificationStatus Status { get; private set; }*/

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
        ChannelId = channel.Id;
        StatusId = NotificationStatus.PendingDelivery.Id;
        /*Channel = channel;
        Status = NotificationStatus.PendingDelivery;*/
    }

    public void MarkDelivered(string? externalMessageId = null)
    {
        StatusId = NotificationStatus.Delivered.Id;
        //Status = NotificationStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
        ExternalMessageId = externalMessageId;
        FailureReason = null;
    }

    public void MarkFailed(string failureReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);
        StatusId = NotificationStatus.Cancelled.Id;
        //Status = NotificationStatus.Cancelled;
        FailureReason = failureReason;
    }
}