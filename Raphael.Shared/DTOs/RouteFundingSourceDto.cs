using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class RouteFundingSourceDto
    {
        // Only the ID of the financing source to associate is necessary.
        [Required]
        [Range(1, int.MaxValue)]
        public int FundingSourceId { get; set; }
    }
}

