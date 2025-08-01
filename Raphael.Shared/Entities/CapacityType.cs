using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class CapacityType
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public ICollection<Vehicle> Vehicles { get; set; }
    }
}

