namespace Raphael.Notification.Application.DTOs;

public class CreateNotificationRequest
{
    public string BusinessEventCode { get; set; }


    public string Title { get; set; }


    public string Message { get; set; }


    public string Priority { get; set; }


    public string Severity { get; set; }


    public string Type { get; set; }


    public DateTime? ExpiresAtUtc { get; set; }


    public List<CreateNotificationRecipientRequest> Recipients { get; set; } = new();


    public List<CreateNotificationActionRequest> Actions { get; set; } = new();
}



public class CreateNotificationRecipientRequest
{
    public Guid RecipientId { get; set; }


    public string RecipientType { get; set; }
}



public class CreateNotificationActionRequest
{
    public string ActionCode { get; set; }


    public int SortOrder { get; set; }


    public bool IsPrimary { get; set; }
}