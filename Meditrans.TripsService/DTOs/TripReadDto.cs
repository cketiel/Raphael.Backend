namespace Meditrans.TripsService.DTOs
{
    public class TripReadDto
    {
        public int Id { get; set; }
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string PickupAddress { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public string DropoffAddress { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public int SpaceTypeId { get; set; }
        public string SpaceTypeName { get; set; }
        public string? PickupNote { get; set; }
        public bool IsCancelled { get; set; }
    }


}
