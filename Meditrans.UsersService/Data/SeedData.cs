using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raphael.UsersService.Models;
using System;
using System.Linq;
using Raphael.UsersService.Helpers;

namespace Raphael.UsersService.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new UsersDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<UsersDbContext>>());

            // Check if any users already exist
            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            context.Users.AddRange(
                new User
                {
                    FullName = "John Doe",
                    Username = "johndoe",
                    PasswordHash = PasswordHasher.Hash("123456"), // hashed! // ?? En producción debe estar hasheado
                    Email = "john@example.com",
                    Phone = "1234567890",
                    Address = "123 Main St",
                    DriverLicense = "A123456",
                    IsActive = true
                },
                new User
                {
                    FullName = "Admin User",
                    Username = "admin",
                    PasswordHash = PasswordHasher.Hash("adminpass"),
                    Email = "admin@Raphael.com",
                    Phone = "9876543210",
                    Address = "456 Admin Rd",
                    DriverLicense = "B654321",
                    IsActive = true
                }
            );

            context.SaveChanges();
        }
    }
}

