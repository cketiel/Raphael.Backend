using System.ComponentModel.DataAnnotations;

namespace Meditrans.Shared.DTOs
{
    // For routing request
    public class RouteTripRequest
    {
        [Required]
        public int VehicleRouteId { get; set; }
        [Required]
        public List<int> TripIds { get; set; }
    }
}
