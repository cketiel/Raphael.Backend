using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities.Notifications;

public class Notification
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Business Event that originated this notification.
    /// Example: DRIVER_ROUTE_MODIFIED
    /// </summary>
    public string BusinessEventCode { get; private set; }
    public int PriorityId { get; private set; }

    public int SeverityId { get; private set; }

    public int TypeId { get; private set; }

    public int StatusId { get; private set; }

    [NotMapped]
    public NotificationPriority Priority
    => Enumeration.FromId<NotificationPriority>(PriorityId);
    [NotMapped]
    public NotificationSeverity Severity
        => Enumeration.FromId<NotificationSeverity>(SeverityId);
    [NotMapped]
    public NotificationType Type
        => Enumeration.FromId<NotificationType>(TypeId);
    [NotMapped]
    public NotificationStatus Status
        => Enumeration.FromId<NotificationStatus>(StatusId);

    /*public NotificationPriority Priority { get; private set; }

    public NotificationSeverity Severity { get; private set; }

    public NotificationType Type { get; private set; }

    public NotificationStatus Status { get; private set; }*/

    public string Title { get; private set; }

    public string Message { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ExpiresAtUtc { get; private set; }

    public ICollection<NotificationRecipient> Recipients { get; private set; }

    public ICollection<NotificationDelivery> Deliveries { get; private set; }

    public ICollection<NotificationMetadata> Metadata { get; private set; }

    public ICollection<NotificationAction> Actions { get; private set; }

    private Notification()
    {
        // Required by EF Core
        Recipients = new List<NotificationRecipient>();

        Deliveries = new List<NotificationDelivery>();

        Metadata = new List<NotificationMetadata>();

        Actions = new List<NotificationAction>();
    }

    public Notification(
        string businessEventCode,
        NotificationPriority priority,
        NotificationSeverity severity,
        NotificationType type,
        string title,
        string message,
        DateTime? expiresAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(businessEventCode);
        ArgumentNullException.ThrowIfNull(priority);
        ArgumentNullException.ThrowIfNull(severity);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Id = Guid.NewGuid();
        BusinessEventCode = businessEventCode;
        PriorityId = priority.Id;
        SeverityId = severity.Id;
        TypeId = type.Id;
        StatusId = NotificationStatus.Created.Id;
        /*Priority = priority;
        Severity = severity;
        Type = type;
        Status = NotificationStatus.Created;
        Title = title;*/
        Message = message;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        Recipients = new List<NotificationRecipient>();

        Deliveries = new List<NotificationDelivery>();

        Metadata = new List<NotificationMetadata>();

        Actions = new List<NotificationAction>();
    }
}