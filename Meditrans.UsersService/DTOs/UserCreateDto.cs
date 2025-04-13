namespace Meditrans.UsersService.DTOs
{
    public class UserCreateDto
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; }

        // Optional
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? DriverLicense { get; set; }
    }
}
