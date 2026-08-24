namespace Raphael.Shared.Entities.Notifications;

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


    public Guid GroupId { get; private set; }


    public BusinessEventGroup Group { get; private set; }


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
        BusinessEventGroup group,
        string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ArgumentNullException.ThrowIfNull(group);

        ArgumentException.ThrowIfNullOrWhiteSpace(source);


        Id = Guid.NewGuid();

        Code = code;

        Name = name;

        Description = description;

        Group = group;

        GroupId = group.Id;

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

    public void SetActive(bool active)
    {
        if (IsActive == active)
            return;

        IsActive = active;
    }

    /// <summary>
    /// Refreshes the descriptive fields from the catalog. The code is the identity and
    /// never changes: renaming it would orphan every rule and every stored notification
    /// that points at it.
    /// </summary>
    public void Update(
        string name,
        string description,
        string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        Name = name;

        Description = description;

        Source = source;
    }
}