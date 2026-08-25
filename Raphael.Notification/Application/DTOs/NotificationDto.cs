namespace Raphael.Notification.Application.DTOs;

/// <summary>
/// One notification as a client application sees it.
/// </summary>
/// <remarks>
/// <see cref="Title"/> and <see cref="Message"/> are the English text the server rendered:
/// it is what a push carries. The in-app inbox is rendered from <see cref="Metadata"/>
/// instead, so a user who switches language sees their whole history switch with them.
/// Leaving the metadata out, as this DTO used to, stored the message key in the database
/// and never let it reach anybody.
/// </remarks>
public class NotificationDto
{
    public Guid Id { get; set; }


    public string BusinessEventCode { get; set; }


    public string Priority { get; set; }


    public string Severity { get; set; }


    public string Type { get; set; }


    public string Status { get; set; }


    public string Title { get; set; }


    public string Message { get; set; }


    public DateTime CreatedAtUtc { get; set; }


    public DateTime? ExpiresAtUtc { get; set; }


    public List<NotificationRecipientDto> Recipients { get; set; } = new();


    public List<NotificationActionDto> Actions { get; set; } = new();


    /// <summary>
    /// Message key, its parameters and the identifiers the notification is about.
    /// Keys are listed in <c>NotificationMetadataKeys</c>. Never contains PHI.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}