using Meditrans.UsersService.Helpers;
using Meditrans.UsersService.Models;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.UsersService.Data
{
    public static class DbInitializer
    {
        public static void Seed2(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            if (!db.Users.Any())
            {
                db.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = "admin", // ⚠️ En texto plano por ahora
                    Role = UserRole.Admin,
                    FullName = "System Administrator"
                });
                db.SaveChanges();
            }
            
        }

        public static void Seed(UsersDbContext context)
        {
            // Aplica migraciones pendientes
            context.Database.Migrate();

            // Verifica si ya hay usuarios
            if (context.Users.Any()) return;

            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    PasswordHash = PasswordHasher.Hash("admin"), // BCrypt.Net.BCrypt.HashPassword("admin"),
                    Role = UserRole.Admin,
                    FullName = "System Administrator",
                    IsActive = true
                },
                new User
                {
                    Username = "driver",
                    PasswordHash = PasswordHasher.Hash("driver"), // BCrypt.Net.BCrypt.HashPassword("driver"),
                    Role = UserRole.Driver,
                    FullName = "Driver for Testing",
                    DriverLicense = "A123456",
                    IsActive = true
                },
                 new User
                {
                    Username = "client",
                    PasswordHash = PasswordHasher.Hash("client"), // BCrypt.Net.BCrypt.HashPassword("client"),
                    Role = UserRole.Client,
                    FullName = "Client for Testing",
                    Email = "client@example.com",
                    Phone = "1234567890",
                    Address = "123 Main St",
                    IsActive = true
                },
                 new User
                {
                    Username = "user",
                    PasswordHash = PasswordHasher.Hash("user"), // BCrypt.Net.BCrypt.HashPassword("user"),
                    Role = UserRole.User,
                    FullName = "User for Testing",
                    Email = "user@example.com",
                    Phone = "9999999999",
                    Address = "999 Main St",
                    IsActive = true
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }

    }

}
