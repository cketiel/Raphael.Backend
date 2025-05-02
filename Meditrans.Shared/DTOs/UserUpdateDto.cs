namespace Meditrans.Shared.DTOs
{
    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public int RoleId { get; set; }

        // Opcionales
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? DriverLicense { get; set; }

        public bool IsActive { get; set; }
    }
}
