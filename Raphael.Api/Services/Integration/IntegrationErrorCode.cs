namespace Raphael.Api.Services.Integration
{
    /// <summary>
    /// Reasons a trip can be rejected, as the integrator sees them.
    /// </summary>
    /// <remarks>
    /// These strings are part of the public integration contract. An integrator branches
    /// on them, so renaming one is a breaking change even though nothing in this solution
    /// would fail to compile.
    /// </remarks>
    public static class IntegrationErrorCode
    {
        /// <summary>The same patient already has an active trip on that date, route and window.</summary>
        public const string DuplicateActiveTrip = "DUPLICATE_ACTIVE_TRIP";

        /// <summary>The trip exists but was cancelled, and a sync will not reinstate it.</summary>
        public const string TripCancelled = "TRIP_CANCELLED";

        /// <summary>Not enough was sent to tell this patient apart from another of the same name.</summary>
        public const string PatientNotIdentifiable = "PATIENT_NOT_IDENTIFIABLE";

        /// <summary>The external TripId is missing or blank.</summary>
        public const string InvalidTripId = "INVALID_TRIP_ID";

        /// <summary>The RiderId sent already belongs to a different patient.</summary>
        public const string DuplicateRider = "DUPLICATE_RIDER_ID";

        /// <summary>Some other uniqueness rule rejected the record.</summary>
        public const string DuplicateRecord = "DUPLICATE_RECORD";

        /// <summary>The trip points at something that does not exist.</summary>
        public const string InvalidReference = "INVALID_REFERENCE";

        /// <summary>A field the trip cannot be stored without arrived empty.</summary>
        public const string MissingRequiredField = "MISSING_REQUIRED_FIELD";

        /// <summary>A text field is longer than the column accepts.</summary>
        public const string FieldTooLong = "FIELD_TOO_LONG";

        /// <summary>Another operation touched the same rows. Retryable.</summary>
        public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

        /// <summary>The database did not answer in time. Retryable.</summary>
        public const string Timeout = "TIMEOUT";

        /// <summary>Cause not safe to state, or not understood. Support resolves it by correlation id.</summary>
        public const string Internal = "INTERNAL_ERROR";
    }
}
