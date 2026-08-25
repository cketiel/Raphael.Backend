namespace Raphael.Shared.Time;

/// <summary>
/// What time it is where the work happens.
/// </summary>
/// <remarks>
/// There are two kinds of time in Raphael and they need opposite treatment.
///
/// <para>
/// <b>Instants</b> — when something happened: a trip was cancelled, a notification was
/// raised. Those are <see cref="UtcNow"/>, stored in UTC, and every application shows them
/// in the timezone of whoever is looking. A dispatcher in another region sees their own
/// hour and nothing has to be configured for that to work.
/// </para>
///
/// <para>
/// <b>Business wall-clock</b> — what time the trip is: <c>Trip.Date</c>, <c>FromTime</c>,
/// <c>ToTime</c>, <c>TripLog.Time</c>. Those are <b>not</b> instants. 09:15 means 09:15 at
/// the pickup address, and it stays 09:15 no matter who opens the screen.
/// </para>
///
/// <para>
/// ⚠️ Converting a trip time into the viewer's timezone would be a serious mistake. A
/// dispatcher in Los Angeles looking at an Ave Maria trip has to read 09:15, not 06:15 —
/// reading 06:15 sends a vehicle three hours early. That is why these methods take the
/// provider, not the user.
/// </para>
///
/// <para>
/// Nothing here ever consults the timezone of the machine running the API.
/// </para>
/// </remarks>
public interface IOperationClock
{
    /// <summary>The current instant. The same everywhere, and safe to store.</summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// The timezone a provider's trips are operated in. Null means the broker's own trips.
    /// </summary>
    TimeZoneInfo ZoneFor(int? providerId);

    /// <summary>Wall-clock date and time where this provider operates.</summary>
    DateTime NowFor(int? providerId);

    /// <summary>Today's date where this provider operates.</summary>
    DateTime TodayFor(int? providerId);

    /// <summary>Time of day where this provider operates.</summary>
    TimeSpan TimeOfDayFor(int? providerId);

    /// <summary>Turns an instant into wall-clock time where this provider operates.</summary>
    DateTime ToOperation(DateTime utc, int? providerId);

    /// <summary>
    /// Forgets the cached provider timezones. Call after a provider is saved, so a
    /// correction takes effect without waiting for the cache to lapse.
    /// </summary>
    void InvalidateZones();
}
