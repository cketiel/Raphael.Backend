namespace Raphael.Shared.DTOs
{
    /// <summary>What happened to one row of an imported file.</summary>
    public static class TripImportStatus
    {
        /// <summary>The trip did not exist and was inserted.</summary>
        public const string Created = "Created";

        /// <summary>The trip already existed under this TripId and was overwritten.</summary>
        public const string Updated = "Updated";

        /// <summary>The row was rejected. Nothing was stored for it.</summary>
        public const string Failed = "Failed";
    }

    /// <summary>
    /// Outcome of a single row inside an import chunk.
    /// </summary>
    /// <remarks>
    /// A batch reports per row because a batch fails per row. The office needs to know which
    /// trips did not go in and why, so they can be found in the original file and corrected.
    /// </remarks>
    public class TripImportItemResultDto
    {
        /// <summary>The broker's identifier, echoed back so rows can be matched to the file.</summary>
        public string TripId { get; set; } = string.Empty;

        /// <summary>One of <see cref="TripImportStatus"/>.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Stable reason code. Null unless the row failed.</summary>
        public string? ErrorCode { get; set; }

        /// <summary>What is wrong with the row, in business terms. Null unless the row failed.</summary>
        public string? Message { get; set; }

        /// <summary>True when importing the same row again could succeed.</summary>
        public bool? Retryable { get; set; }

        /// <summary>
        /// Identifier of the server-side record of this failure.
        /// </summary>
        /// <remarks>
        /// The cause stays in the log in full and only a key to it crosses the wire, because
        /// a database error message on this schema quotes patient data.
        /// </remarks>
        public string? CorrelationId { get; set; }
    }

    /// <summary>Result of one import chunk.</summary>
    public class TripImportResultDto
    {
        /// <summary>True only when every row in the chunk was stored.</summary>
        public bool Success { get; set; }

        /// <summary>Human-readable summary of the chunk.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>How many rows were stored as new trips.</summary>
        public int CreatedCount { get; set; }

        /// <summary>How many rows overwrote an existing trip.</summary>
        public int UpdatedCount { get; set; }

        /// <summary>How many rows were rejected.</summary>
        public int FailedCount { get; set; }

        /// <summary>When the chunk was processed, in UTC.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>One entry per row received, in the order they were sent.</summary>
        public List<TripImportItemResultDto> Results { get; set; } = new();
    }
}
