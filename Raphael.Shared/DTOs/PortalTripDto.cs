using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class PortalTripDto : IntegrationTripDto
    {
        // Id interno de la DB para actualizaciones desde la web
        public int? InternalId { get; set; }

        // Lógica de Round Trip
        public bool IsRoundTrip { get; set; }
        public TimeSpan? ReturnTime { get; set; }

        // Campos obligatorios de Customer que podrían faltar en el DTO base
        public DateTime? CustomerDOB { get; set; }
    }
}
