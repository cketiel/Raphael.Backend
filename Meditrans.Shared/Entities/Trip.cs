namespace Meditrans.Shared.Entities
{
    public class Trip
    {
        public int Id { get; set; }
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public string PickupAddress { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public string DropoffAddress { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }
        public string PickupNote { get; set; }
        public bool IsCancelled { get; set; }
        public ICollection<Schedule> Schedules { get; set; }
    }
}
