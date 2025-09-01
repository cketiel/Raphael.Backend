using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class UnitDto
    {
        [Required]
        [StringLength(10)]
        public string Abbreviation { get; set; }

        [Required]
        [StringLength(100)]
        public string Description { get; set; }
    }
}
