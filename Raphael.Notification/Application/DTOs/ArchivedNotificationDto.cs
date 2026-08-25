namespace Raphael.Notification.Application.DTOs;

/// <summary>
/// An archived notification, as the administration panel lists it.
/// </summary>
/// <remarks>
/// Archived rows are the ones the cleanup will never remove, so this is the only list in
/// the system that grows without a ceiling. Somebody has to be able to look at it and
/// decide what still deserves to be there.
/// </remarks>
public sealed class ArchivedNotificationDto
{
    public Guid Id { get; set; }

    public string BusinessEventCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>Which applications this notice was addressed to.</summary>
    public List<string> Audiences { get; set; } = [];

    /// <summary>Who decided to keep it, from the audit trail. Null for older rows.</summary>
    public string? ArchivedByUsername { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }
}

/// <summary>
/// The archived notifications of one application.
/// </summary>
public sealed class ArchivedNotificationGroupDto
{
    /// <summary>Driver, Rider, Desktop User or Integration.</summary>
    public string Audience { get; set; } = string.Empty;

    public int Count { get; set; }

    public List<ArchivedNotificationDto> Items { get; set; } = [];
}

/// <summary>
/// Everything archived, grouped by the application it was addressed to.
/// </summary>
/// <remarks>
/// ⚠️ <see cref="Total"/> is not the sum of the groups. One notification can be addressed
/// to several applications — a cancellation reaches the patient, the office and the driver
/// — and it appears under each of them while being one row.
/// </remarks>
public sealed class ArchivedNotificationsDto
{
    public int Total { get; set; }

    public List<ArchivedNotificationGroupDto> Groups { get; set; } = [];
}
