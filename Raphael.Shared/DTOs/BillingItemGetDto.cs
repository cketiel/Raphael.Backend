namespace Raphael.Shared.DTOs
{
    public class BillingItemGetDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool IsCopay { get; set; }
        public string? ARAccount { get; set; }
        public string? ARSubAccount { get; set; }
        public string? ARCompany { get; set; }
        public string? APAccount { get; set; }
        public string? APSubAccount { get; set; }
        public string? APCompany { get; set; }
        public string UnitAbbreviation { get; set; }
        public int UnitId { get; set; }

        /// <summary>The clinic that keeps this item in the Booking Portal; null for the office's own.</summary>
        public int? OwnerIntegratorId { get; set; }

        /// <summary>That clinic's name, so the office can group the items without another call.</summary>
        public string? OwnerIntegratorName { get; set; }
    }
}
