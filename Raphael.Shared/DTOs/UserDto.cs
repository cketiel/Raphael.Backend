namespace Raphael.Shared.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? DriverLicense { get; set; }
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
    }
}

