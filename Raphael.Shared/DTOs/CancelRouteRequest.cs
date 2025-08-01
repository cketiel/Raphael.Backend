using System.ComponentModel.DataAnnotations;

namespace Meditrans.Shared.DTOs
{
    // For cancellation request
    public class CancelRouteRequest
    {
        [Required]
        public int ScheduleId { get; set; }
    }
}
