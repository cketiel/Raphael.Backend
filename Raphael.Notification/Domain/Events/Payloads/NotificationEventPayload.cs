namespace Raphael.Notification.Domain.Events.Payloads;

public class NotificationEventPayload
{
    public Guid Id { get; private set; }


    /// <summary>
    /// Business event identifier.
    /// Example: DRIVER_ROUTE_MODIFIED
    /// </summary>
    public string EventCode { get; private set; }


    /// <summary>
    /// Date and time when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; private set; }


    /// <summary>
    /// Application that generated the event.
    /// Example: Raphael.Desktop
    /// </summary>
    public string Source { get; private set; }


    /// <summary>
    /// Dynamic event data.
    /// Example:
    /// DriverId = 25
    /// TripId = 1000
    /// </summary>
    public Dictionary<string, string> Data { get; private set; }


    private NotificationEventPayload()
    {
        // Required by EF Core
        Data = new();
    }


    public NotificationEventPayload(
        string eventCode,
        string source,
        Dictionary<string, string> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventCode);

        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        ArgumentNullException.ThrowIfNull(data);


        Id = Guid.NewGuid();

        EventCode = eventCode;

        Source = source;

        OccurredAt = DateTime.UtcNow;

        Data = data;
    }


    public string? GetValue(string key)
    {
        if (Data.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }
}