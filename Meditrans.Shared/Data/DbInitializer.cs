using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Meditrans.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.Shared.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly RaphaelContext _db;

        public DbInitializer(RaphaelContext db)
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
       
    }
}
