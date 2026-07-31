namespace Raphael.Notification.Application.Commands.ProcessBusinessEvent;

public class ProcessBusinessEventCommand
{
    public string BusinessEventCode { get; }


    public Guid EntityId { get; }


    public string EntityType { get; }


    public Dictionary<string, object> Data { get; }


    public ProcessBusinessEventCommand(
        string businessEventCode,
        Guid entityId,
        string entityType,
        Dictionary<string, object> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(businessEventCode);

        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        ArgumentNullException.ThrowIfNull(data);


        BusinessEventCode = businessEventCode;

        EntityId = entityId;

        EntityType = entityType;

        Data = data;
    }
}