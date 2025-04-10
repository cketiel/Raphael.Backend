using Meditrans.UsersService.Data;
using Meditrans.UsersService.Models;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.UsersService.Services
{
    public class UserService : IUserService
    {
       
        private readonly List<User> _users = new();
      

        public IEnumerable<User> GetAll() => _users;
        

        public User? GetById(Guid id) => _users.FirstOrDefault(u => u.Id == id);

        public void Create(User user)
        {
            user.Id = Guid.NewGuid();
            _users.Add(user);
        }

        public void Update(User user)
        {
            var existing = GetById(user.Id);
            if (existing == null) return;

            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.Phone = user.Phone;
            existing.Address = user.Address;
            existing.DriverLicense = user.DriverLicense;
            existing.Role = user.Role;
            existing.IsActive = user.IsActive;
        }

        public void Delete(Guid id)
        {
            var user = GetById(id);
            if (user != null)
                _users.Remove(user);
        }
    }
}
