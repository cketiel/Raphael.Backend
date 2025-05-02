using Meditrans.Shared.DbContexts;
using Meditrans.UsersService.Helpers;
using Meditrans.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.UsersService.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly MediTransContext _db;

        public DbInitializer(MediTransContext db)
        {
            _db = db;

        }
        public void Initialize()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate(); // Run pending migrations
                }

            }
            catch (Exception)
            {

                throw;
            }


            // Create roles, users, etc. //if(_db.Roles.Any(r => r.RoleName == "Admin"))return;
            //if (_db.Roles.Any()) return; // _roleService.CreateAsync(new Role("Admin")).GetAwaiter().GetResult();
            // If no roles exist, then create the Administrator role
            var adminRole = new Role();
            var driverRole = new Role();
            var userRole = new Role();
            if (!_db.Roles.Any())
            {
                var roles = new List<Role>();

                adminRole.Id = 1;
                adminRole.RoleName = "Admin";
                adminRole.Description = "System Administrator";

                driverRole.Id = 2;
                driverRole.RoleName = "Driver";
                driverRole.Description = "Driver Role";

                userRole.Id = 3;
                userRole.RoleName = "User";
                userRole.Description = "User Role";

                roles.Add(adminRole);
                roles.Add(driverRole);
                roles.Add(userRole);

                _db.Roles.AddRange(roles);
                _db.SaveChanges();
            }
                
            //if (_db.Users.Any()) return;
            if (!_db.Users.Any())
            {
                var users = new List<User>
                {
                    new User {
                        FullName = "System Administrator",
                        Username = "Admin",
                        PasswordHash = PasswordHasher.Hash("admin"),
                        RoleId = adminRole.Id,
                        IsActive = true
                    },
                    new User {
                        FullName = "Driver for Testing",
                        Username = "Driver",
                        PasswordHash = PasswordHasher.Hash("diver"),
                        DriverLicense = "A123456",
                        RoleId = driverRole.Id,
                        IsActive = true
                    },
                    new User {
                        FullName = "User for Testing",
                        Username = "User",
                        PasswordHash = PasswordHasher.Hash("user"),
                        RoleId = userRole.Id,
                        Email = "user@example.com",
                        PhoneNumber = "9999999999",
                        Address = "999 Main St",
                        IsActive = true
                    }
                };

                _db.Users.AddRange(users);
                _db.SaveChanges();

            }            

        }

        /*public static void Seed2(IApplicationBuilder app)
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
        }*/

    }

}
