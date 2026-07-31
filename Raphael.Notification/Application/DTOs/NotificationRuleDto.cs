namespace Raphael.Notification.Application.DTOs;

public class NotificationRuleDto
{
    public Guid Id { get; set; }


    public string Code { get; set; }


    public string Name { get; set; }


    public string Description { get; set; }


    public string NotificationType { get; set; }


    public string Priority { get; set; }


    public string Severity { get; set; }


    public bool IsActive { get; set; }


    public string BusinessEventCode { get; set; }
}