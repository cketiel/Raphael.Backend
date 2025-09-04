namespace Raphael.Shared.Entities
{
    public class FundingSourceBillingItem
    {
        public int Id { get; set; }
        public int FundingSourceId { get; set; }
        public FundingSource FundingSource { get; set; }
        public int BillingItemId { get; set; }
        public BillingItem BillingItem { get; set; } 
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }
        public decimal Rate { get; set; }
        public string? Per { get; set; }
        public bool IsDefault { get; set; }
        public string? ProcedureCode { get; set; }
        public decimal? MinCharge { get; set; }
        public decimal? MaxCharge { get; set; }
        public int? GreaterThanMinQty { get; set; }
        public int? LessOrEqualMaxQty { get; set; }
        public int? FreeQty { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}

