using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class Integrator
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } // Example: "Ryde Central"
        [Required]
        public string ApiKey { get; set; } // rc_live_...
        public bool IsActive { get; set; } = true;
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
