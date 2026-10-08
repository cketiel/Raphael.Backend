using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class PortalTripDto : IntegrationTripDto
    {
        // --- PROPERTY SHADOWING ---
        // We use 'new' to override the base class definition.
        // This removes the RegularExpression restriction of AMB|WCH|STR,
        // but we keep the [Required] attribute if it remains mandatory for the web.
        [Required]
        public new string SpaceTypeName { get; set; }

        // Id interno de la DB para actualizaciones desde la web
        public int? InternalId { get; set; }

        // Lógica de Round Trip
        public bool IsRoundTrip { get; set; }
        public TimeSpan? ReturnTime { get; set; }

        // Campos obligatorios de Customer que podrían faltar en el DTO base
        public DateTime? CustomerDOB { get; set; }

        public string? RoundTripPickupComment { get; set; }
        public string? RoundTripDropoffComment { get; set; }

        /// <summary>
        /// The Provider the clinic gives the trip to; null is the super broker. Applied only when
        /// <see cref="SetProvider"/> is true: the old Booking Web never sends it, and reading its
        /// silence as "super broker" would wipe a Provider the office had assigned.
        /// </summary>
        public int? ProviderId { get; set; }

        public bool SetProvider { get; set; }
    }
}
