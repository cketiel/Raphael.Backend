namespace Raphael.Notification.Domain.Models;

public class NotificationMetadata
{
    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }

    public string Key { get; private set; }

    public string Value { get; private set; }


    private NotificationMetadata()
    {
        // Required by EF Core
    }


    public NotificationMetadata(
        Guid notificationId,
        string key,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        ArgumentException.ThrowIfNullOrWhiteSpace(value);


        Id = Guid.NewGuid();

        NotificationId = notificationId;

        Key = key;

        Value = value;
    }
}