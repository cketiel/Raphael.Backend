using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities
{
    
    public class TripAttachment
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        [ForeignKey("TripId")]
        public virtual Trip Trip { get; set; }

        public string FileName { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = null!;
        public string ContentType { get; set; } = string.Empty;
        public string? NotificationEmail { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
