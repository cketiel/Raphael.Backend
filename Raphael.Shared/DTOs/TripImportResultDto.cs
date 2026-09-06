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

        /// <summary>
        /// The trip this row collided with. Null unless <see cref="ErrorCode"/> says it clashed.
        /// </summary>
        /// <remarks>
        /// Only the import fills this in, and only for a dispatcher holding a JWT. It is not on
        /// the integrator path and must not be put there: it describes a record the integrator
        /// may not own, and naming somebody else's booking to them is a disclosure. The office
        /// already sees all of it on the trips grid.
        /// </remarks>
        public TripImportConflictDto? Conflict { get; set; }
    }

    /// <summary>
    /// The trip that was already there, when a row is refused for duplicating one.
    /// </summary>
    /// <remarks>
    /// "This is a duplicate" is not an answer anybody can act on: the dispatcher has to know
    /// WHICH trip, or they have to go and hunt for it. Duplicate here means the same patient on
    /// the same day between the same two addresses in the same window - so those are the fields,
    /// plus the identifier the existing trip is filed under, which is how it gets found again.
    /// </remarks>
    public class TripImportConflictDto
    {
        /// <summary>The broker identifier the existing trip is filed under.</summary>
        public string? TripId { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan? FromTime { get; set; }

        public TimeSpan? ToTime { get; set; }

        public string? PatientName { get; set; }

        public string? PickupAddress { get; set; }

        public string? DropoffAddress { get; set; }

        /// <summary>Where the existing trip has got to, so a started journey is not overwritten.</summary>
        public string? Status { get; set; }
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
