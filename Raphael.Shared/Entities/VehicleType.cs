using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class VehicleType
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}

