using Raphael.Shared.Entities;

namespace Raphael.Shared.DTOs
{
    public class ScheduleHistoryDto
    {        
        public int Id { get; set; }
        public string Name { get; set; }
        public TimeSpan? Perform { get; set; } // Time it was completed
        public TimeSpan? ScheduledTime { get; set; } // Scheduled time (for cancelled)
        public string Patient { get; set; }
        public string Address { get; set; }
        public string CityStateZip { get; set; }
        public ScheduleEventType? EventType { get; set; }
        public string TripType { get; set; }    

        public bool IsCanceled { get; set; }
    }
}
