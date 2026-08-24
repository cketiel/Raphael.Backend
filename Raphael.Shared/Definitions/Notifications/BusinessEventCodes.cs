namespace Raphael.Shared.Definitions.Notifications;

/// <summary>
/// Business events actually published by the system.
/// </summary>
/// <remarks>
/// The catalog declares around 150 events; only the ones listed here are wired.
/// An event earns its place when its absence makes somebody take a wrong decision:
/// a dispatcher assigning a route to a trip that no longer exists, or a patient
/// waiting inside the clinic without knowing the vehicle is already at the door.
///
/// <para>Naming rule: the code is a verb in the past tense.</para>
/// </remarks>
public static class BusinessEventCodes
{
    /// <summary>A trip got a route and a vehicle assigned.</summary>
    public const string TripScheduled = "TRIP_SCHEDULED";

    /// <summary>
    /// A trip was cancelled. Who cancelled it travels in the payload, under
    /// <see cref="BusinessEventDataKeys.CancelledBy"/>: modelling one event per origin
    /// would mean six almost identical sets of rules, and the audience is the same.
    /// </summary>
    public const string TripCancelled = "TRIP_CANCELLED";

    /// <summary>The driver took the trip and is on the way to the pickup address.</summary>
    public const string DriverStartedTrip = "DRIVER_STARTED_TRIP";

    /// <summary>The driver reached the pickup address and is waiting for the patient.</summary>
    public const string DriverArrivedPickup = "DRIVER_ARRIVED_PICKUP";

    /// <summary>The patient boarded and the vehicle is heading to the dropoff address.</summary>
    public const string DriverPickedUpPassenger = "DRIVER_PICKED_UP_PASSENGER";

    /// <summary>The driver left the patient at the destination.</summary>
    public const string DriverCompletedTrip = "DRIVER_COMPLETED_TRIP";

    /// <summary>
    /// The patient reported being ready for a trip booked without a pickup time.
    /// From this instant the office has one hour to get a vehicle there.
    /// </summary>
    public const string WillCallActivated = "WILL_CALL_ACTIVATED";

    /// <summary>
    /// A dispatcher took charge of a Will Call. Chained from the acknowledgement of
    /// <see cref="WillCallActivated"/>, so the patient learns the office knows.
    /// </summary>
    public const string WillCallAcknowledged = "WILL_CALL_ACKNOWLEDGED";
}
