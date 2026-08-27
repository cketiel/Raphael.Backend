namespace Raphael.Shared.Definitions.Notifications;

/// <summary>
/// Keys of the dictionary handed to <c>NotificationService.PublishAsync</c>.
/// </summary>
/// <remarks>
/// Recipient keys carry weight beyond naming: the factory drops any recipient whose
/// identifier is not in the payload. That is how "tell the integration only if the trip
/// is theirs" and "tell the driver only if the trip is under way" are expressed, without
/// a single rule condition. Leaving a key out is a deliberate act, not an omission.
/// </remarks>
public static class BusinessEventDataKeys
{
    // ---------------------------------------------------------------
    // Recipients. Their presence decides who gets notified.
    // ---------------------------------------------------------------

    /// <summary>CustomerId of the patient. Reaches Raphael.Rider.</summary>
    public const string RiderId = "RiderId";

    /// <summary>UserId of the driver. Include it only when the trip is under way.</summary>
    public const string DriverId = "DriverId";

    /// <summary>UserId of one concrete dispatcher. Rarely used: the office is addressed as an audience.</summary>
    public const string DesktopUserId = "DesktopUserId";

    /// <summary>IntegratorId. Include it only when the trip belongs to that integration.</summary>
    public const string IntegrationId = "IntegrationId";

    // ---------------------------------------------------------------
    // Context of the event
    // ---------------------------------------------------------------

    /// <summary>Identifier of the trip the event is about.</summary>
    public const string TripId = "TripId";

    /// <summary>The trip entity, when the message needs data from it.</summary>
    public const string Trip = "Trip";

    /// <summary>Estimated travel time to the pickup address.</summary>
    public const string Travel = "Travel";

    /// <summary>Which kind of actor cancelled. See <see cref="CancelledByTypes"/>.</summary>
    public const string CancelledBy = "CancelledBy";

    /// <summary>Reason given for the cancellation, when there is one.</summary>
    public const string CancellationReason = "CancellationReason";

    /// <summary>Internal user who performed the action, so the office can hide its own notice.</summary>
    public const string PerformedByUserId = "PerformedByUserId";

    /// <summary>Instant a Will Call was activated. The one hour commitment counts from here.</summary>
    public const string WillCallActivatedAtUtc = "WillCallActivatedAtUtc";

    /// <summary>Absolute deadline by which a vehicle must reach the patient.</summary>
    public const string WillCallDeadlineUtc = "WillCallDeadlineUtc";

    /// <summary>
    /// Whether the trip entered or left the driver's route. See <c>RouteChangeTypes</c>.
    /// Travels with <c>DRIVER_ROUTE_UPDATED</c>.
    /// </summary>
    public const string RouteChange = "RouteChange";
}

/// <summary>
/// Kinds of actor that can cancel a trip. Travels under
/// <see cref="BusinessEventDataKeys.CancelledBy"/>.
/// </summary>
public static class CancelledByTypes
{
    /// <summary>An office user, from Raphael.Desktop.</summary>
    public const string Dispatcher = "DISPATCHER";

    /// <summary>The assigned driver, from Raphael.Driver. Recorded as a no show.</summary>
    public const string Driver = "DRIVER";

    /// <summary>The patient, from Raphael.Rider.</summary>
    public const string Rider = "RIDER";

    /// <summary>A clinic, from the Raphael Booking Portal.</summary>
    public const string Facility = "FACILITY";

    /// <summary>An external system, through its API Key.</summary>
    public const string Integrator = "INTEGRATOR";

    /// <summary>The customer service bot.</summary>
    public const string Bot = "BOT";
}
