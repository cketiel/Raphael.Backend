namespace Raphael.Shared.DTOs
{
    public class TripHistoryCreateDto
    {
        public int TripId { get; set; }
        public string User { get; set; }
        public string Field { get; set; }
        public string? PriorValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime? ChangeDate { get; set; }
    }
}
