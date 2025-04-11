using Meditrans.Shared.DbContexts;
using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;
using Meditrans.UsersService.Data;
using Meditrans.UsersService.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.UsersService.Services
{
    public class UserService : IUserService
    {
        private readonly MediTransContext _context;

        public UserService(MediTransContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RoleName = u.Role.RoleName
                })
                .ToListAsync();
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = user.Role.RoleName
            };
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> UpdateAsync(int id, User user)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return null;

            existing.FullName = user.FullName;
            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.PhoneNumber = user.PhoneNumber;
            existing.Address = user.Address;
            existing.RoleId = user.RoleId;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return false;

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
                return false;

            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
            await _context.SaveChangesAsync();

            return true;
        }
    }

    /*public class UserService : IUserService
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
    }*/
}
