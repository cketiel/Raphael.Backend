using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? DriverLicense { get; set; }
        public bool IsActive { get; set; }
        [Required]
        public int RoleId { get; set; }
        public Role Role { get; set; }
        //public ICollection<VehicleRoute> VehicleRoutes { get; set; }
    }
}
