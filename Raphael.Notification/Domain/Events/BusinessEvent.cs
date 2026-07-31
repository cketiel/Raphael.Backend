namespace Raphael.Notification.Domain.Events;

public class BusinessEvent
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Unique identifier used by applications and services.
    /// Example: DRIVER_ROUTE_MODIFIED
    /// </summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }


    public Guid CategoryId { get; private set; }

    public BusinessEventCategory Category { get; private set; }


    /// <summary>
    /// Application or service that generated the event.
    /// Example: Raphael.Api, Raphael.Driver
    /// </summary>
    public string Source { get; private set; }


    public bool IsActive { get; private set; }


    private BusinessEvent()
    {
        // Required by EF Core
    }


    public BusinessEvent(
        string code,
        string name,
        string description,
        BusinessEventCategory category,
        string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ArgumentNullException.ThrowIfNull(category);

        ArgumentException.ThrowIfNullOrWhiteSpace(source);


        Id = Guid.NewGuid();

        Code = code;

        Name = name;

        Description = description;

        Category = category;

        CategoryId = category.Id;

        Source = source;

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