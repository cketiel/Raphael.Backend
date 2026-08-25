namespace Raphael.Notification.Application.DTOs;

public class NotificationRecipientDto
{
    public Guid Id { get; set; }


    public Guid RecipientId { get; set; }


    public string RecipientType { get; set; }


    /// <summary>
    /// True when this row addresses a whole audience instead of one person.
    /// </summary>
    /// <remarks>
    /// A dispatch office notice is stored once and read by everyone, so its viewed and
    /// acknowledged marks belong to the office, not to the dispatcher who happened to
    /// open it. A client that treats a broadcast as personal would clear the unread mark
    /// for its eleven colleagues. Exposed as a flag so no client has to hardcode the
    /// audience Guid.
    /// </remarks>
    public bool IsBroadcast { get; set; }


    public string Status { get; set; }


    public DateTime? DeliveredAtUtc { get; set; }


    public DateTime? ViewedAtUtc { get; set; }


    public DateTime? AcknowledgedAtUtc { get; set; }
}