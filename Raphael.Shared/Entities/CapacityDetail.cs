using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class CapacityDetail
    {
        public int Id { get; set; }
        [Required]
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }
        [Required]
        public int CapacityDetailTypeId { get; set; }
        public CapacityDetailType CapacityDetailType { get; set; }
        public int Quantity { get; set; } = 0;
    }
}

