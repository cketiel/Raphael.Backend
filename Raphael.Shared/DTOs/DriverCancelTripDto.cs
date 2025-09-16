using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class DriverCancelTripDto
    {
        [Required]
        public string Reason { get; set; }
    }
}
