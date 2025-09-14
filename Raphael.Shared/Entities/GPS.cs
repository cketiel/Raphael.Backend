using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class GPS
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdVehicleRoute { get; set; }

        public DateTime DateTime { get; set; }

        public double Speed { get; set; }

        public string? Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? Direction { get; set; }
    }
}
