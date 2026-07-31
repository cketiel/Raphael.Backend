namespace Raphael.Notification.Application.DTOs;

public class NotificationRecipientDto
{
    public Guid Id { get; set; }


    public Guid RecipientId { get; set; }


    public string RecipientType { get; set; }


    public string Status { get; set; }


    public DateTime? DeliveredAtUtc { get; set; }


    public DateTime? ViewedAtUtc { get; set; }


    public DateTime? AcknowledgedAtUtc { get; set; }
}