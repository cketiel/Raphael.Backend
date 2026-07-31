namespace Raphael.Notification.Application.Interfaces.Events;

public sealed class BusinessEventContext
{
    /// <summary>
    /// Business event identifier (example: DRIVER_ROUTE_MODIFIED).
    /// </summary>
    public required string EventCode { get; init; }

    /// <summary>
    /// Entity that originated the event.
    /// Example: TripId, VehicleRouteId, CustomerId...
    /// </summary>
    public Guid AggregateId { get; init; }

    /// <summary>
    /// User that originated the event.
    /// </summary>
    public Guid? PerformedByUserId { get; init; }

    /// <summary>
    /// UTC date/time when the event occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional information required by notification rules.
    /// </summary>
    public Dictionary<string, object?> Data { get; } = new();
}