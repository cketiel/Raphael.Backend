using Raphael.Shared.Entities;

namespace Raphael.Shared.DTOs
{
    public class FundingSourceBillingItemDto
    {   
        public int Id { get; set; }
        public int FundingSourceId { get; set; }
        public int BillingItemId { get; set; }
        public int SpaceTypeId { get; set; }
        public decimal Rate { get; set; }
        public string? Per { get; set; }
        public bool IsDefault { get; set; }
        public string? ProcedureCode { get; set; }
        public decimal MinCharge { get; set; }
        public decimal MaxCharge { get; set; }
        public int GreaterThanMinQty { get; set; }
        public int LessOrEqualMaxQty { get; set; }
        public int FreeQty { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}

