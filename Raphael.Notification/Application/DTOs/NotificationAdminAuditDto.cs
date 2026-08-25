namespace Raphael.Notification.Application.DTOs;

/// <summary>
/// One entry of the notification administration trail.
/// </summary>
public sealed class NotificationAdminAuditDto
{
    public Guid Id { get; set; }

    /// <summary>One of <c>NotificationAdminActions</c>: the client translates it.</summary>
    public string Action { get; set; } = string.Empty;

    public int? PerformedByUserId { get; set; }

    public string PerformedByUsername { get; set; } = string.Empty;

    public DateTime PerformedAtUtc { get; set; }

    public Guid? NotificationId { get; set; }

    public int AffectedCount { get; set; }

    public string? Details { get; set; }
}
