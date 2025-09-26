using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class ProviderDto
    {       

        [Required(ErrorMessage = "The provider name is required.")]
        [StringLength(150)]
        public string Name { get; set; }

        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Logo { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
