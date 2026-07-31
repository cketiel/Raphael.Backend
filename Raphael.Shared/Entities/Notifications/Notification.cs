using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Shared.Entities.Notifications;

public class Notification
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Business Event that originated this notification.
    /// Example: DRIVER_ROUTE_MODIFIED
    /// </summary>
    public string BusinessEventCode { get; private set; }

    public NotificationPriority Priority { get; private set; }

    public NotificationSeverity Severity { get; private set; }

    public NotificationType Type { get; private set; }

    public NotificationStatus Status { get; private set; }

    public string Title { get; private set; }

    public string Message { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ExpiresAtUtc { get; private set; }

    private Notification()
    {
        // Required by EF Core
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
        Priority = priority;
        Severity = severity;
        Type = type;
        Status = NotificationStatus.Created;
        Title = title;
        Message = message;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }
}