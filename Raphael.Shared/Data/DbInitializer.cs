using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Shared.Data
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
                if (_db.Database.GetPendingMigrations().Any())
                {
                    _db.Database.Migrate(); // Run pending migrations
                }
            }
            catch (Exception)
            {               
                throw;
            }

            // If roles do not exist, create them
            if (!_db.Roles.Any())
            {
                var adminRole = new Role
                {
                    RoleName = "Admin",
                    Description = "System Administrator"
                };

                var driverRole = new Role
                {
                    RoleName = "Driver",
                    Description = "Driver Role"
                };

                var userRole = new Role
                {
                    RoleName = "User",
                    Description = "User Role"
                };

                // Add the roles to the context
                _db.Roles.AddRange(adminRole, driverRole, userRole);
                // Save the changes so that the database assigns the IDs
                _db.SaveChanges();

                // If no users exist, create them
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
                            PasswordHash = PasswordHasher.Hash("driver"), 
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
}