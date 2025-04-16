using System.ComponentModel.DataAnnotations;

namespace Meditrans.Shared.Entities
{
    public class VehicleRoute
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Required]
        public int DriverId { get; set; }
        public User Driver { get; set; }
        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }
        public string? Garage { get; set; }
        public ICollection<Schedule> Schedules { get; set; }
    }
}
