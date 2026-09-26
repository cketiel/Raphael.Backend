namespace Raphael.Shared.Entities.CallRequests
{
    /// <summary>
    /// A driver asking the dispatch office to call them back. One open case per driver at a time.
    /// </summary>
    /// <remarks>
    /// No foreign keys to Users, VehicleRoutes or Schedules, on purpose: this is history and must
    /// neither block deleting those rows nor disappear with them. Names are copied for the same reason.
    /// </remarks>
    public class DriverCallRequest
    {
        public int Id { get; set; }

        public int DriverId { get; set; }

        public string DriverName { get; set; } = string.Empty;

        public int? VehicleRouteId { get; set; }

        public string? RouteName { get; set; }

        public int? ProviderId { get; set; }

        /// <summary>Business day the request belongs to, in the provider's zone.</summary>
        public DateTime OperatingDate { get; set; }

        /// <summary>The stop the driver had in front of them when they asked.</summary>
        public int? ScheduleId { get; set; }

        public double? RequestLatitude { get; set; }

        public double? RequestLongitude { get; set; }

        public CallRequestStatus Status { get; set; }

        public DateTime RequestedAtUtc { get; set; }

        /// <summary>Position in the queue. Set on creation and on reopening, never by a reminder.</summary>
        public DateTime QueuedAtUtc { get; set; }

        /// <summary>Last time the driver pressed anything; the throttle counts from here.</summary>
        public DateTime LastDriverSignalAtUtc { get; set; }

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

        /// <summary>When it left the open set, for whatever reason.</summary>
        public DateTime? ClosedAtUtc { get; set; }

        public string? ReasonCode { get; set; }

        /// <summary>⚠️ Free text written by a dispatcher: may contain PHI. Never logged, broadcast or pushed.</summary>
        public string? ResolutionNote { get; set; }

        public byte[] RowVersion { get; set; } = [];

        public ICollection<DriverCallRequestEvent> Events { get; set; } = new List<DriverCallRequestEvent>();

        public bool IsOpen => Status is CallRequestStatus.Waiting or CallRequestStatus.InProgress;
    }
}
