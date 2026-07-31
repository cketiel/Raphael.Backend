namespace Raphael.Notification.Application.DTOs;

public class NotificationActionDto
{
    public Guid Id { get; set; }


    public string ActionCode { get; set; }


    public int SortOrder { get; set; }


    public bool IsPrimary { get; set; }
}