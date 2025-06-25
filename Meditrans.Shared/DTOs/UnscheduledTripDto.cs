namespace Meditrans.Shared.DTOs
{
    public class UnscheduledTripDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
        public string PickupAddress { get; set; }
        public string DropoffAddress { get; set; }
        public string SpaceType { get; set; }
        public string FundingSource { get; set; }
        public string? PickupLatitude { get; set; }
        public string? PickupLongitude { get; set; }
        public string? DropoffLatitude { get; set; }
        public string? DropoffLongitude { get; set; }

    }
}
