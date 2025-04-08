namespace Meditrans.TripsService.Models
{
    public class Trip
    {
        public Guid Id { get; set; }
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string PatientName { get; set; }
        public string PickupAddress { get; set; }
        public string DropoffAddress { get; set; }       
        public string SpaceType { get; set; }
        public string PickupNote { get; set; }
        public bool IsCancelled { get; set; }
    }
}
