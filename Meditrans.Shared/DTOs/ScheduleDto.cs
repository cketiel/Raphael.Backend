namespace Meditrans.Shared.DTOs
{
    public class ScheduleDto
    {
        public int Id { get; set; }
        public int? TripId { get; set; }
        public string Name { get; set; }
        public TimeSpan? Pickup { get; set; } // Mapped from ScheduledPickupTime
        public TimeSpan? Appt { get; set; }   // Mapped from ScheduledApptTime
        public TimeSpan? ETA { get; set; }
        public double? Distance { get; set; }
        public TimeSpan? Travel { get; set; }
        public int? On { get; set; } 
        public string Address { get; set; }
        public double ScheduleLatitude { get; set; }
        public double ScheduleLongitude { get; set; }
        public string? Comment { get; set; }
        public string? Phone { get; set; }
        public TimeSpan? Arrive { get; set; }
        public TimeSpan? Perform { get; set; }
        public double? ArriveDist { get; set; }
        public double? PerformDist { get; set; }
        public string? Driver { get; set; }
        public string? GPSArrive { get; set; }
        public long? Odometer { get; set; }
        public string? AuthNo { get; set; }
        public string? FundingSource { get; set; }
        public DateTime? Date { get; set; }
        public int? Sequence { get; set; }
    }
}
