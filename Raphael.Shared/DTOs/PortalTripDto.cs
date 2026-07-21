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
    }
}
