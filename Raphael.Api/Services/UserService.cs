using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Api.Services
{
    public class UserService : IUserService
    {
        private readonly RaphaelContext _context;

        public UserService(RaphaelContext context)
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
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    DriverLicense = u.DriverLicense,
                    IsActive = u.IsActive,
                    RoleId = u.RoleId,
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
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DriverLicense = user.DriverLicense,
                IsActive = user.IsActive,
                RoleId = user.RoleId,
                RoleName = user.Role.RoleName
            };
        }

        public async Task<User> CreateAsync(UserCreateDto dto)
        {
            try
            {
                // Check if that username already exists
                if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                    throw new Exception("Username already exists.");

                // Create the user
                var user = new User
                {
                    FullName = dto.FullName,
                    Username = dto.Username,
                    PasswordHash = PasswordHasher.Hash(dto.Password),
                    RoleId = dto.RoleId,
                    IsActive = true,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    DriverLicense = dto.DriverLicense
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return new User
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Role = user.Role
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating user: {ex.Message}", ex);
            }
        }

        public async Task<User> UpdateAsync(UserUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(dto.Id);
            if (user == null)
                throw new Exception("User not found.");

            // Update the fields
            user.FullName = dto.FullName;
            user.Username = dto.Username;
            user.RoleId = dto.RoleId;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.DriverLicense = dto.DriverLicense;
            user.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                throw new Exception("User not found.");

            // Check current password
            if (!PasswordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new Exception("Current password is incorrect.");

            // Verify that the new password and confirmation match.
            if (dto.NewPassword != dto.ConfirmPassword)
                throw new Exception("New password and confirmation do not match.");

            // Change password
            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
            await _context.SaveChangesAsync();
        }

    }
    
}

