using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class RouteFundingSource
    {
        public int Id { get; set; }

        [Required]
        public int VehicleRouteId { get; set; }
        public VehicleRoute VehicleRoute { get; set; }

        [Required]
        public int FundingSourceId { get; set; }
        public FundingSource FundingSource { get; set; }
    }
}

