namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// What happened to one trip in a synchronization batch.
    /// </summary>
    public static class IntegrationSyncStatus
    {
        /// <summary>The trip did not exist for this integrator and was inserted.</summary>
        public const string Created = "Created";

        /// <summary>The trip already existed for this integrator and was overwritten.</summary>
        public const string Updated = "Updated";

        /// <summary>The trip was rejected. Nothing was stored for it.</summary>
        public const string Failed = "Failed";
    }

    /// <summary>
    /// Outcome of a single trip inside a synchronization batch.
    /// </summary>
    /// <remarks>
    /// A batch reports per trip because a batch fails per trip. One malformed row used to
    /// take the other forty-nine down with it and the integrator was told only that
    /// something, somewhere, had gone wrong.
    /// </remarks>
    public class IntegrationSyncItemResultDto
    {
        /// <summary>The identifier the integrator sent, echoed back so rows can be matched up.</summary>
        public string TripId { get; set; } = string.Empty;

        /// <summary>One of <see cref="IntegrationSyncStatus"/>.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Stable reason code. Null unless the trip failed.</summary>
        public string? ErrorCode { get; set; }

        /// <summary>What is wrong with the trip, in business terms. Null unless the trip failed.</summary>
        public string? Message { get; set; }

        /// <summary>True when sending the same payload again could succeed.</summary>
        public bool? Retryable { get; set; }

        /// <summary>
        /// Identifier of the server-side record of this failure.
        /// </summary>
        /// <remarks>
        /// This is how the true cause is handed over without exposing it: the full detail
        /// sits in our logs under this id, and support can read it out on request.
        /// </remarks>
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// Result of a synchronization batch.
    /// </summary>
    /// <remarks>
    /// <see cref="Success"/>, <see cref="Message"/>, <see cref="ProcessedCount"/> and
    /// <see cref="Timestamp"/> keep the names and meanings the endpoint has always
    /// returned, so an integrator reading only those keeps working untouched.
    /// </remarks>
    public class IntegrationSyncResultDto
    {
        /// <summary>True only when every trip in the batch was stored.</summary>
        public bool Success { get; set; }

        /// <summary>Human-readable summary of the batch.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>How many trips were stored.</summary>
        public int ProcessedCount { get; set; }

        /// <summary>How many trips were rejected.</summary>
        public int FailedCount { get; set; }

        /// <summary>When the batch was processed, in UTC.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>One entry per trip received, in the order they were sent.</summary>
        public List<IntegrationSyncItemResultDto> Results { get; set; } = new();
    }
}
