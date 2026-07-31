namespace Raphael.Notification.Application.DTOs;

public class BusinessEventDto
{
    public Guid Id { get; set; }


    public string Code { get; set; }


    public string Name { get; set; }


    public string Description { get; set; }


    public string Source { get; set; }


    public bool IsActive { get; set; }
}