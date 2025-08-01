using System.ComponentModel.DataAnnotations;

namespace Meditrans.Shared.Entities
{
    public class VehicleGroup
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        //public ICollection<Vehicle> Vehicles { get; set; }
    }
}
