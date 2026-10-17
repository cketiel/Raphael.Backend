using Raphael.Shared.Interfaces;

namespace Raphael.Shared.Entities
{
    /// <summary>
    /// A vehicle or a driver taken out of service, until somebody puts it back (RE-053).
    /// </summary>
    /// <remarks>
    /// The state lives on the vehicle and on the driver, never on the route: a route is out of
    /// service while its vehicle or its driver has an open record. A vehicle with no record is in
    /// service, which is every vehicle that existed before this table.
    /// <para>
    /// One open record per subject, enforced by the database. Taking out the vehicle and the
    /// driver together writes two records that share <see cref="OperationId"/>.
    /// </para>
    /// </remarks>
    public class OutOfServiceRecord : IOwnedByProvider
    {
        public int Id { get; set; }

        /// <summary>Groups the records and the trip impacts written by one confirmation.</summary>
        public Guid OperationId { get; set; }

        /// <summary><see cref="Definitions.OutOfService.OutOfServiceSubjects"/>.</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>The Vehicle's Id or the driver User's Id, by <see cref="Subject"/>.</summary>
        public int SubjectId { get; set; }

        /// <summary><see cref="Definitions.OutOfService.OutOfServiceReasonCodes"/>.</summary>
        public string ReasonCode { get; set; } = string.Empty;

        public string? ReasonNote { get; set; }

        /// <summary>The route the action was taken from, when it was taken from Runs.</summary>
        public int? OriginVehicleRouteId { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public int? StartedByUserId { get; set; }

        public string? StartedByName { get; set; }

        /// <summary>Null while the subject is out of service.</summary>
        public DateTime? EndedAtUtc { get; set; }

        public int? EndedByUserId { get; set; }

        public string? EndedByName { get; set; }

        public int? OwnerProviderId { get; set; }

        public ICollection<OutOfServiceTripImpact> TripImpacts { get; set; } = new List<OutOfServiceTripImpact>();
    }

    /// <summary>
    /// What an Out of Service operation did to one trip: taken off, kept because the patient was on
    /// board, or kept by a substitution. So it is known in which trip the problem happened.
    /// </summary>
    public class OutOfServiceTripImpact
    {
        public int Id { get; set; }

        public int OutOfServiceRecordId { get; set; }

        public OutOfServiceRecord OutOfServiceRecord { get; set; } = null!;

        public int TripId { get; set; }

        public int VehicleRouteId { get; set; }

        /// <summary>The trip's operating day.</summary>
        public DateTime Date { get; set; }

        /// <summary><see cref="Definitions.OutOfService.OutOfServiceTripOutcomes"/>.</summary>
        public string Outcome { get; set; } = string.Empty;

        public int? OldVehicleId { get; set; }

        public int? NewVehicleId { get; set; }

        public int? OldDriverId { get; set; }

        public int? NewDriverId { get; set; }
    }
}
