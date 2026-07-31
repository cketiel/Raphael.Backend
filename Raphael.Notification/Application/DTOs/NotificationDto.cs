namespace Raphael.Notification.Application.DTOs;

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
}