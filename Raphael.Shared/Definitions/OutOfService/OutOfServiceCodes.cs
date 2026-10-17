namespace Raphael.Shared.Definitions.OutOfService
{
    /// <summary>What is out of service (RE-053). A route is out of service when its vehicle or its driver is.</summary>
    public static class OutOfServiceSubjects
    {
        public const string Vehicle = "VEHICLE";

        public const string Driver = "DRIVER";

        public static bool IsValid(string? code) => code is Vehicle or Driver;
    }

    /// <summary>
    /// Why a vehicle or a driver was taken out of service, chosen by the dispatcher. The note says
    /// the rest; <see cref="Other"/> requires it.
    /// </summary>
    /// <remarks>
    /// ⚠️ A driver's reason is HR data about an employee, and "medical" is health data. The codes
    /// are deliberately neutral, and the note is never logged.
    /// </remarks>
    public static class OutOfServiceReasonCodes
    {
        // ---- Vehicle ----
        public const string MechanicalFailure = "MECHANICAL_FAILURE";
        public const string Accident = "ACCIDENT";
        public const string Maintenance = "MAINTENANCE";
        public const string FailedInspection = "FAILED_INSPECTION";
        public const string RegistrationOrInsuranceExpired = "REGISTRATION_INSURANCE_EXPIRED";
        public const string LiftOrRampFailure = "LIFT_RAMP_FAILURE";
        public const string CleaningOrSanitary = "CLEANING_SANITARY";

        // ---- Driver ----
        public const string MedicalOrPersonalLeave = "MEDICAL_PERSONAL_LEAVE";
        public const string LicenseExpired = "LICENSE_EXPIRED";
        public const string CertificationsExpired = "CERTIFICATIONS_EXPIRED";
        public const string AbsentOrNoShow = "ABSENT_NO_SHOW";
        public const string FamilyEmergency = "FAMILY_EMERGENCY";
        public const string DisciplinaryOrInvestigation = "DISCIPLINARY_INVESTIGATION";
        public const string TechnicalIssue = "TECHNICAL_ISSUE";

        /// <summary>Requires a note: an "other" with no words says nothing about the cause.</summary>
        public const string Other = "OTHER";

        public const int NoteMaxLength = 250;

        public static readonly IReadOnlyList<string> Vehicle = new[]
        {
            MechanicalFailure, Accident, Maintenance, FailedInspection,
            RegistrationOrInsuranceExpired, LiftOrRampFailure, CleaningOrSanitary, Other
        };

        public static readonly IReadOnlyList<string> Driver = new[]
        {
            MedicalOrPersonalLeave, LicenseExpired, CertificationsExpired, AbsentOrNoShow,
            FamilyEmergency, DisciplinaryOrInvestigation, TechnicalIssue, Other
        };

        public static bool IsValid(string subject, string? code) => code is not null && subject switch
        {
            OutOfServiceSubjects.Vehicle => Vehicle.Contains(code),
            OutOfServiceSubjects.Driver => Driver.Contains(code),
            _ => false
        };
    }

    /// <summary>What a Out of Service operation did to one trip (RE-053).</summary>
    public static class OutOfServiceTripOutcomes
    {
        /// <summary>Taken off the route and back to the unscheduled trips.</summary>
        public const string Unrouted = "UNROUTED";

        /// <summary>The patient was on board: the trip stays on the route so it can be finished.</summary>
        public const string ExcludedOnboard = "EXCLUDED_ONBOARD";

        /// <summary>Another vehicle or driver took over the route, so the trip stayed on it.</summary>
        public const string KeptBySubstitution = "KEPT_BY_SUBSTITUTION";
    }
}
