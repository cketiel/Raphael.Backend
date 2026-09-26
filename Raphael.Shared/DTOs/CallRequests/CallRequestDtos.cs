using System;
using System.Collections.Generic;

namespace Raphael.Shared.DTOs.CallRequests
{
    /// <summary>
    /// A call request as every screen shows it. Also travels over the dispatch board hub, so it
    /// carries no patient data and never the resolution note.
    /// </summary>
    /// <remarks>Status is text (Waiting, InProgress, Resolved, Cancelled, Expired) so client copies cannot drift on enum numbers.</remarks>
    public class CallRequestSummaryDto
    {
        public int Id { get; set; }

        public string Status { get; set; } = string.Empty;

        public int DriverId { get; set; }

        public string DriverName { get; set; } = string.Empty;

        public int? VehicleRouteId { get; set; }

        public string? RouteName { get; set; }

        public DateTime OperatingDate { get; set; }

        public DateTime RequestedAtUtc { get; set; }

        public DateTime QueuedAtUtc { get; set; }

        public int ReminderCount { get; set; }

        public DateTime? LastReminderAtUtc { get; set; }

        public int CallAttempts { get; set; }

        public DateTime? LastAttemptAtUtc { get; set; }

        public DateTime? DriverAvailableAtUtc { get; set; }

        public int? ClaimedByUserId { get; set; }

        public string? ClaimedByName { get; set; }

        public DateTime? ClaimedAtUtc { get; set; }

        public int? ResolvedByUserId { get; set; }

        public string? ResolvedByName { get; set; }

        public DateTime? ClosedAtUtc { get; set; }

        public string? ReasonCode { get; set; }

        /// <summary>1 for the driver's first request of that day, 2 for the second, and so on.</summary>
        public int RequestNumberOfDay { get; set; }

        /// <summary>Grows with every change. A screen ignores anything older than what it shows.</summary>
        public long Revision { get; set; }
    }

    /// <summary>Everything the dispatcher needs on screen before calling the driver back.</summary>
    public class CallRequestDetailDto
    {
        public CallRequestSummaryDto Request { get; set; } = new();

        public string? DriverPhone { get; set; }

        public string? VehicleName { get; set; }

        /// <summary>⚠️ May contain PHI. Only here, for the office: never in a list, a message or an export.</summary>
        public string? ResolutionNote { get; set; }

        public CallRequestRouteContextDto? Route { get; set; }

        public CallRequestPositionDto? RequestPosition { get; set; }

        public CallRequestPositionDto? LastPosition { get; set; }

        public List<CallRequestTimelineItemDto> Timeline { get; set; } = new();

        public List<CallRequestSummaryDto> OtherRequestsOfDay { get; set; } = new();
    }

    /// <summary>How the driver's day is going, on the route the request belongs to.</summary>
    public class CallRequestRouteContextDto
    {
        public bool PulledOut { get; set; }

        public TimeSpan? PulledOutAt { get; set; }

        public bool PulledIn { get; set; }

        public int StopsDone { get; set; }

        public int StopsTotal { get; set; }

        public CallRequestStopDto? LastPerformed { get; set; }

        public CallRequestStopDto? Next { get; set; }

        public CallRequestStopDto? AtRequest { get; set; }
    }

    /// <summary>A stop, by identifiers and times only.</summary>
    public class CallRequestStopDto
    {
        public int ScheduleId { get; set; }

        public int? TripId { get; set; }

        /// <summary>Pickup, Dropoff, PullOut or PullIn.</summary>
        public string Kind { get; set; } = string.Empty;

        public TimeSpan? ScheduledTime { get; set; }

        public TimeSpan? Eta { get; set; }

        /// <summary>ETA minus the scheduled time. Negative is early.</summary>
        public int? MinutesLate { get; set; }

        public bool Performed { get; set; }

        public TimeSpan? PerformedAt { get; set; }
    }

    public class CallRequestPositionDto
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime? AtUtc { get; set; }

        public double? Speed { get; set; }

        public string? Address { get; set; }
    }

    public class CallRequestTimelineItemDto
    {
        public string Type { get; set; } = string.Empty;

        public DateTime AtUtc { get; set; }

        public string? ByName { get; set; }

        public string? Detail { get; set; }
    }

    /// <summary>What the driver's app needs to draw its button.</summary>
    public class DriverCallRequestDto
    {
        public bool HasOpenRequest { get; set; }

        public int? Id { get; set; }

        public string? Status { get; set; }

        public DateTime? RequestedAtUtc { get; set; }

        public int ReminderCount { get; set; }

        public DateTime? LastReminderAtUtc { get; set; }

        public string? ClaimedByFirstName { get; set; }

        public DateTime? ClaimedAtUtc { get; set; }

        public int CallAttempts { get; set; }

        public DateTime? LastAttemptAtUtc { get; set; }

        /// <summary>The office tried to call and the driver has not said they can talk since.</summary>
        public bool MissedCallPending { get; set; }

        /// <summary>Null when the driver may press again right now.</summary>
        public DateTime? NextSignalAllowedAtUtc { get; set; }

        /// <summary>False when the press came inside the throttle window and changed nothing.</summary>
        public bool SignalAccepted { get; set; }

        /// <summary>The number the office will call. Empty means the office has none on file.</summary>
        public string? CallbackPhone { get; set; }

        /// <summary>The server's clock, so the app's countdown does not depend on the phone's.</summary>
        public DateTime ServerTimeUtc { get; set; }
    }

    public class CreateCallRequestDto
    {
        public int? VehicleRouteId { get; set; }

        public int? ScheduleId { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }

    public class ResolveCallRequestDto
    {
        public string ReasonCode { get; set; } = string.Empty;

        public string? Note { get; set; }
    }

    /// <summary>Body of a 409: the request changed under the caller, and this is how it stands now.</summary>
    public class CallRequestConflictDto
    {
        public string Message { get; set; } = string.Empty;

        public CallRequestSummaryDto? Current { get; set; }
    }
}
