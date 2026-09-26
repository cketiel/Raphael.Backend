namespace Raphael.Shared.Definitions.CallRequests
{
    /// <summary>
    /// Why a call request was closed, chosen by the dispatcher. The raw data for normalising the
    /// causes before the driver is ever asked to pick one.
    /// </summary>
    public static class CallRequestReasonCodes
    {
        public const string Address = "ADDRESS";

        public const string PatientLocation = "PATIENT_LOCATION";

        public const string Vehicle = "VEHICLE";

        public const string RouteSchedule = "ROUTE_SCHEDULE";

        public const string NotNeeded = "NOT_NEEDED";

        /// <summary>Requires a note: an "other" with no words says nothing about the cause.</summary>
        public const string Other = "OTHER";

        public const int NoteMaxLength = 250;

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            Address,
            PatientLocation,
            Vehicle,
            RouteSchedule,
            NotNeeded,
            Other
        };

        public static bool IsValid(string? code) => code is not null && All.Contains(code);
    }
}
