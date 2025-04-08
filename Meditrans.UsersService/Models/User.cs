namespace Meditrans.UsersService.Models
{
    public enum UserRole
    {
        Admin,
        Driver,
        Client
    }

    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string DriverLicense { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public UserRole Role { get; set; }
    }
}
