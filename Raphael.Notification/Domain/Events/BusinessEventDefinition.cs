namespace Raphael.Notification.Domain.Events;

public class BusinessEventDefinition
{
    public Guid Id { get; private set; }


    public Guid BusinessEventId { get; private set; }


    public BusinessEvent BusinessEvent { get; private set; }


    /// <summary>
    /// Unique event identifier published by the system.
    /// Example: DRIVER_ROUTE_MODIFIED
    /// </summary>
    public string Code { get; private set; }


    public string DisplayName { get; private set; }


    public string Description { get; private set; }


    /// <summary>
    /// Indicates if this event can generate notifications.
    /// </summary>
    public bool GeneratesNotification { get; private set; }


    public bool IsActive { get; private set; }


    private BusinessEventDefinition()
    {
        // Required by EF Core
    }


    public BusinessEventDefinition(
        BusinessEvent businessEvent,
        string code,
        string displayName,
        string description,
        bool generatesNotification = true)
    {
        ArgumentNullException.ThrowIfNull(businessEvent);

        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        ArgumentException.ThrowIfNullOrWhiteSpace(description);


        Id = Guid.NewGuid();

        BusinessEvent = businessEvent;

        BusinessEventId = businessEvent.Id;

        Code = code;

        DisplayName = displayName;

        Description = description;

        GeneratesNotification = generatesNotification;

        IsActive = true;
    }


    public void Disable()
    {
        IsActive = false;
    }


    public void Enable()
    {
        IsActive = true;
    }
}