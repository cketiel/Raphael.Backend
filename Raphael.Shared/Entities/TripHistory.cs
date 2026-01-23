using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Raphael.Shared.Entities
{
    public class TripHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TripId { get; set; } 

        [Required]
        public DateTime ChangeDate { get; set; }

        [Required]
        public string User { get; set; }

        [Required]
        public string Field { get; set; }

        public string? PriorValue { get; set; }

        public string? NewValue { get; set; }

        [ForeignKey("TripId")]
        [JsonIgnore]
        public virtual Trip Trip { get; set; }
    }
}
