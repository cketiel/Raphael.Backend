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
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public double? Distance { get; set; }

    }
}
