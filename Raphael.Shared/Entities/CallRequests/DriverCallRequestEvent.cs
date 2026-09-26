namespace Raphael.Shared.Entities.CallRequests
{
    /// <summary>
    /// Append-only timeline of a call request: who did what and when. Also the source of its metrics.
    /// </summary>
    public class DriverCallRequestEvent
    {
        public long Id { get; set; }

        public int CallRequestId { get; set; }

        public DriverCallRequest CallRequest { get; set; } = null!;

        public CallRequestEventType Type { get; set; }

        public DateTime AtUtc { get; set; }

        public int? ByUserId { get; set; }

        public string? ByName { get; set; }

        /// <summary>A reason code or the previous holder's name. Never free text from a person.</summary>
        public string? Detail { get; set; }
    }
}
