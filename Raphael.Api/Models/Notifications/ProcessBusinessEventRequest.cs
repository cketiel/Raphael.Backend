namespace Raphael.Api.Models.Notifications;

public class ProcessBusinessEventRequest
{
    public string BusinessEventCode { get; set; }


    public Guid EntityId { get; set; }


    public string EntityType { get; set; }


    public Dictionary<string, object> Data { get; set; }
}