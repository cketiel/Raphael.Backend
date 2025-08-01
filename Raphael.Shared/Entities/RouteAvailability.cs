using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class RouteAvailability
    {
        public int Id { get; set; }

        [Required]
        public int VehicleRouteId { get; set; }
        public VehicleRoute VehicleRoute { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
