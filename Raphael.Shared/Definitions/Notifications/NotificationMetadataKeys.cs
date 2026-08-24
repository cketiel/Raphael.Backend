namespace Raphael.Shared.Definitions.Notifications;

/// <summary>
/// Keys stored in <c>NotificationMetadata</c> alongside every notification.
/// </summary>
/// <remarks>
/// The stored <c>Title</c> and <c>Message</c> are English text: they are what a push
/// carries, since the server renders it. The in-app inbox is translated by each
/// application from <see cref="MessageKey"/> plus these parameters, so a user switching
/// language sees their whole history switch with them.
///
/// <para>
/// No patient name, address or phone number is ever placed here. What travels is the
/// TripId; the application loads the detail once the user opens it, already
/// authenticated.
/// </para>
/// </remarks>
public static class NotificationMetadataKeys
{
    /// <summary>Resource key the client applications translate.</summary>
    public const string MessageKey = "MessageKey";

    /// <summary>Trip the notification is about. Lets the app open the right screen.</summary>
    public const string TripId = "TripId";

    /// <summary>
    /// Patient the notification concerns. Written only on the Will Call notice the
    /// dispatch office receives, so that acknowledging it can reach the right patient
    /// without going back to the database. An internal identifier, never a name.
    /// </summary>
    public const string RiderId = "RiderId";

    /// <summary>Trip date, ISO 8601.</summary>
    public const string TripDate = "TripDate";

    /// <summary>Pickup time, HH:mm.</summary>
    public const string TripTime = "TripTime";

    /// <summary>Kind of actor that cancelled. See <see cref="CancelledByTypes"/>.</summary>
    public const string CancelledBy = "CancelledBy";

    /// <summary>
    /// Internal user who performed the action, when there is one.
    /// </summary>
    /// <remarks>
    /// Office notices are stored once for the whole dispatch office, so the backend
    /// cannot leave the author out of a broadcast the way it does with an individual
    /// recipient. Raphael.Desktop uses this to hide the notice from whoever caused it:
    /// they already saw the confirmation on screen.
    /// </remarks>
    public const string PerformedByUserId = "PerformedByUserId";

    /// <summary>Minutes left until the vehicle reaches the pickup address.</summary>
    public const string EtaMinutes = "EtaMinutes";

    /// <summary>Instant the Will Call was activated, ISO 8601.</summary>
    public const string WillCallActivatedAtUtc = "WillCallActivatedAtUtc";

    /// <summary>Deadline to get a vehicle to the patient, ISO 8601.</summary>
    public const string WillCallDeadlineUtc = "WillCallDeadlineUtc";
}
