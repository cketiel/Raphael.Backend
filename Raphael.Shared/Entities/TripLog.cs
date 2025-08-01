using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class TripLog
    {
        public int Id { get; set; }
        [Required]
        public int TripId { get; set; }
        public Trip Trip { get; set; }
        [Required]
        public string Status { get; set; } = string.Empty;
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public TimeSpan Time { get; set; }
    }
}

