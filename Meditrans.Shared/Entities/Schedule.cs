namespace Meditrans.Shared.Entities
{
    public class Schedule
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public Trip Trip { get; set; }
        public int VehicleRouteId { get; set; }
        public VehicleRoute VehicleRoute { get; set; }
        public TimeSpan ETA { get; set; }
        public decimal Distance { get; set; }
    }
}
