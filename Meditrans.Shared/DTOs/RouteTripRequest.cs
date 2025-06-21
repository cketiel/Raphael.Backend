using System.ComponentModel.DataAnnotations;

namespace Meditrans.Shared.DTOs
{
    public class RouteTripRequest
    {
        [Required]
        public int VehicleRouteId { get; set; }
        [Required]
        public List<int> TripIds { get; set; }
    }
}
