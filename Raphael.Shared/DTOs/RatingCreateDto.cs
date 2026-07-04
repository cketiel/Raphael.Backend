namespace Raphael.Shared.DTOs
{
    public class RatingCreateDto
    {
        public int TripId { get; set; }
        public int CustomerId { get; set; }
        public int DriverId { get; set; }
        public double Score { get; set; }
        public string? Comment { get; set; }
    }
}
