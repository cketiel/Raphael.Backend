namespace Raphael.Notification.Domain.Events;

public class BusinessEventGroup
{
    public Guid Id { get; private set; }

    public Guid CategoryId { get; private set; }

    public BusinessEventCategory Category { get; private set; }


    /// <summary>
    /// Unique identifier of the group.
    /// Example: TRIP_EXECUTION
    /// </summary>
    public string Code { get; private set; }


    public string Name { get; private set; }


    public string Description { get; private set; }


    public bool IsActive { get; private set; }


    private BusinessEventGroup()
    {
        // Required by EF Core
    }


    public BusinessEventGroup(
        BusinessEventCategory category,
        string code,
        string name,
        string description)
    {
        ArgumentNullException.ThrowIfNull(category);

        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(description);


        Id = Guid.NewGuid();

        Category = category;

        CategoryId = category.Id;

        Code = code;

        Name = name;

        Description = description;

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