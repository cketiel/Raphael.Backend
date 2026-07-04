namespace Raphael.Shared.DTOs
{
    public class RatingReadDto : RatingCreateDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string DriverName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
