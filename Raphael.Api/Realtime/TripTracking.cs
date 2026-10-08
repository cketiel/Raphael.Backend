using Raphael.Shared.Entities;

namespace Raphael.Api.Realtime
{
    /// <summary>
    /// When a clinic may see the vehicle carrying one of its trips.
    /// </summary>
    /// <remarks>
    /// Only while the trip is under way: the driver heading to the pickup, at the door, or carrying
    /// the patient. Before and after, the same vehicle is serving other patients and its position
    /// would show where they live (HIPAA). <see cref="TripStatus.Late"/> is left out: it can be set
    /// while the vehicle is still busy with someone else's trip.
    /// </remarks>
    public static class TripTracking
    {
        public static readonly IReadOnlyList<string> UnderWayStatuses =
            new[] { TripStatus.Started, TripStatus.Arrived, TripStatus.InProgress };

        public static bool IsUnderWay(string? status) =>
            status != null && UnderWayStatuses.Contains(status);
    }
}
