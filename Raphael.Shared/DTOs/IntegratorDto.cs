using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class IntegratorDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "The Name is required")]
        public string Name { get; set; }
        public string? ApiKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        // Extra property to know if the client wants to refresh the key
        public bool RegenerateApiKey { get; set; }
    }
}