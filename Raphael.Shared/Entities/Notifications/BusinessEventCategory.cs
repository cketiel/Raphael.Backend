namespace Raphael.Shared.Entities.Notifications;

public sealed class BusinessEventCategory
{
    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }


    private BusinessEventCategory()
    {
        // Required by EF Core
    }


    public BusinessEventCategory(
        string code,
        string name,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);


        Id = Guid.NewGuid();

        Code = code;

        Name = name;

        Description = description;
    }
}