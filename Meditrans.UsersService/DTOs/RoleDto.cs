using System.ComponentModel.DataAnnotations;

namespace Meditrans.UsersService.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string? Description { get; set; }
    }
}
