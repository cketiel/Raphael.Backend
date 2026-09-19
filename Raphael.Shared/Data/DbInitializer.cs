using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Raphael.Shared.Data
{
    public class DbInitializer : IDbInitializer
    {
        /// <summary>
        /// Whether the API applies pending migrations as it starts. Default true.
        /// </summary>
        /// <remarks>
        /// True is what this has always done, so nothing changes by leaving it alone. It is a
        /// key so that it can be turned off where changing the schema on a restart is not
        /// wanted -- an environment holding a restore of real data, for one -- without that
        /// being a code change made in a hurry.
        /// </remarks>
        public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

        private readonly RaphaelContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(
            RaphaelContext db,
            IConfiguration configuration,
            ILogger<DbInitializer> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public void Initialize()
        {
            if (_configuration.GetValue(MigrateOnStartupKey, true))
            {
                var pending = _db.Database.GetPendingMigrations().ToList();

                if (pending.Count > 0)
                {
                    // Said out loud before it happens. A deployment that changes the shape of
                    // the database should not be something you reconstruct afterwards from
                    // whether anything broke.
                    _logger.LogWarning(
                        "Applying {Count} pending migration(s): {Migrations}",
                        pending.Count,
                        string.Join(", ", pending));

                    _db.Database.Migrate();
                }
            }
            else
            {
                var pending = _db.Database.GetPendingMigrations().ToList();

                if (pending.Count > 0)
                {
                    // Not an error: somebody asked for this. But an API running against a
                    // schema older than its code is worth one line at startup.
                    _logger.LogWarning(
                        "{Key} is off and {Count} migration(s) are pending: {Migrations}. " +
                        "The database is behind this build.",
                        MigrateOnStartupKey,
                        pending.Count,
                        string.Join(", ", pending));
                }
            }

            SeedRoles();
            SeedUsers();
        }

        private void SeedRoles()
        {
            if (_db.Roles.Any())
            {
                return;
            }

            _db.Roles.AddRange(
                new Role { RoleName = "Admin", Description = "System Administrator" },
                new Role { RoleName = "Driver", Description = "Driver Role" },
                new Role { RoleName = "User", Description = "User Role" });

            _db.SaveChanges();
        }

        /// <remarks>
        /// ⚠️ Guarded on Users, not on Roles. It used to be nested inside the roles check, so a
        /// database that had roles and no users could never acquire any -- and that is exactly
        /// the state the first Azure database ended up in on 2026-09-18: the roles saved, the
        /// users threw on an unmapped NOT NULL column, and the roles check then said "already
        /// seeded" on every later start. Nobody could sign in and nothing said why.
        /// </remarks>
        private void SeedUsers()
        {
            if (_db.Users.Any())
            {
                return;
            }

            var roles = _db.Roles.ToDictionary(r => r.RoleName, r => r.Id);

            _db.Users.AddRange(
                new User
                {
                    FullName = "System Administrator",
                    Username = "Admin",
                    PasswordHash = PasswordHasher.Hash("admin"),
                    RoleId = roles["Admin"],
                    IsActive = true
                },
                new User
                {
                    FullName = "Driver for Testing",
                    Username = "Driver",
                    PasswordHash = PasswordHasher.Hash("driver"),
                    DriverLicense = "A123456",
                    RoleId = roles["Driver"],
                    IsActive = true
                },
                new User
                {
                    FullName = "User for Testing",
                    Username = "User",
                    PasswordHash = PasswordHasher.Hash("user"),
                    RoleId = roles["User"],
                    Email = "user@example.com",
                    PhoneNumber = "9999999999",
                    Address = "999 Main St",
                    IsActive = true
                });

            _db.SaveChanges();

            // These three passwords are in a public repository. They are a starting point for
            // an empty database, not credentials: change them the first time anybody signs in.
            _logger.LogWarning(
                "Seeded the three starter accounts on an empty database. Their passwords are " +
                "the ones written in DbInitializer, which is public. Change them now.");
        }
    }
}
