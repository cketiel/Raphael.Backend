using System.Data;
using System.Threading;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.Helpers;
using Microsoft.Data.SqlClient;
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

        /// <summary>
        /// How long a single migration statement may take, in seconds. Default 300.
        /// </summary>
        /// <remarks>
        /// The 30 seconds Entity Framework uses by default is a reasonable answer for a query
        /// and a bad one for an <c>ALTER TABLE</c> over a table holding years of trips. A
        /// migration that times out leaves the schema half-applied and the API refusing to
        /// start, which is the most expensive way to discover a number.
        /// </remarks>
        public const string CommandTimeoutKey = "Database:CommandTimeoutSeconds";

        /// <summary>
        /// How long an instance waits for another one to finish initialising, in seconds.
        /// Default 300.
        /// </summary>
        public const string LockTimeoutKey = "Database:LockTimeoutSeconds";

        /// <summary>
        /// Name the instances contend on. An application lock is scoped to the database, so
        /// two instances pointed at the same one queue behind each other, and DEV and PROD --
        /// different databases on different servers -- never see each other's lock.
        /// </summary>
        private const string LockResource = "Raphael:DbInitializer";

        /// <summary>
        /// Azure SQL drops connections: a failover moves the database, a maintenance window
        /// closes a socket. None of that is a reason to fail a deployment, so a transient
        /// fault is retried before it is believed. Everything this class does is idempotent,
        /// so a retry that repeats work already done is safe.
        /// </summary>
        private const int MaxAttempts = 5;

        private const int DefaultCommandTimeoutSeconds = 300;
        private const int DefaultLockTimeoutSeconds = 300;

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
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    InitializeOnce();
                    return;
                }
                catch (Exception ex) when (IsTransient(ex) && attempt < MaxAttempts)
                {
                    var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt));

                    _logger.LogWarning(
                        ex,
                        "Transient SQL fault while initialising the database (attempt {Attempt} " +
                        "of {MaxAttempts}). Retrying in {Backoff}.",
                        attempt,
                        MaxAttempts,
                        backoff);

                    Thread.Sleep(backoff);
                }
            }
        }

        private void InitializeOnce()
        {
            var commandTimeout = ReadTimeout(CommandTimeoutKey, DefaultCommandTimeoutSeconds);
            _db.Database.SetCommandTimeout(commandTimeout);

            //
            // A second connection, held open for as long as this takes, whose only job is to
            // own an application lock. Two instances overlap during a restart or a scale-out,
            // and neither half of this is safe run twice at once: two processes running the
            // same ALTER TABLE is one, and the seeding below is the other -- it reads "are
            // there any roles?" and writes if not, which is a race with a name.
            //
            // Null for a provider with no connection string, and for anything that is not SQL
            // Server there is no sp_getapplock to call. Both cases do the work unguarded,
            // which is what happened on every start before today.
            //
            SqlConnection? gate = null;

            if (_db.Database.IsSqlServer() && _db.Database.GetConnectionString() is { } connectionString)
            {
                gate = new SqlConnection(connectionString);
            }

            // Released only if it was taken. Without this, a startup refused BECAUSE another
            // instance holds the lock would go on to release one it never owned, and the
            // failure that matters would be followed in the log by a second, meaningless one.
            var holdsLock = false;

            try
            {
                if (gate is not null)
                {
                    gate.Open();
                    AcquireLock(gate);
                    holdsLock = true;
                }

                MigrateIfWanted(gate);
                SeedRoles();
                SeedUsers();
            }
            finally
            {
                if (gate is not null)
                {
                    if (holdsLock)
                    {
                        ReleaseLock(gate);
                    }

                    gate.Dispose();
                }
            }
        }

        private void MigrateIfWanted(SqlConnection? gate)
        {
            //
            // Read the pending list AFTER the lock above, never before. An instance that
            // queued there has just waited for another one to finish exactly this work, so a
            // list read on the way in would be stale.
            //
            var pending = _db.Database.GetPendingMigrations().ToList();

            if (!_configuration.GetValue(MigrateOnStartupKey, true))
            {
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

                return;
            }

            if (pending.Count == 0)
            {
                return;
            }

            RefuseUnmanagedSchema(gate, pending.Count);

            // Said out loud before it happens. A deployment that changes the shape of the
            // database should not be something you reconstruct afterwards from whether
            // anything broke.
            _logger.LogWarning(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count,
                string.Join(", ", pending));

            var startedAt = DateTimeOffset.UtcNow;
            _db.Database.Migrate();

            _logger.LogWarning(
                "Applied {Count} migration(s) in {Elapsed:F1}s. The schema is now at {Latest}.",
                pending.Count,
                (DateTimeOffset.UtcNow - startedAt).TotalSeconds,
                pending[^1]);
        }

        /// <summary>
        /// Refuses to migrate a database that already has tables but no record of how they
        /// got there.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Entity Framework reads an absent <c>__EFMigrationsHistory</c> as "no migrations
        /// applied", which on a database that already has tables means it considers every
        /// migration pending and starts creating the whole schema from the first one onwards
        /// -- against tables full of patients. It fails partway, having already run some of
        /// it, and the way back is a restore.
        /// </para>
        /// <para>
        /// The case this is written for is the cutover: PROD is restored from a backup of
        /// MyASP.NET, whose schema Entity Framework built and whose history table therefore
        /// comes with it. Low odds. The cost of being wrong is the whole database, and the
        /// check is one query.
        /// </para>
        /// </remarks>
        private void RefuseUnmanagedSchema(SqlConnection? gate, int pendingCount)
        {
            if (gate is null || _db.Database.GetAppliedMigrations().Any())
            {
                return;
            }

            using var command = gate.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM sys.tables " +
                "WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory';";

            var tables = Convert.ToInt32(command.ExecuteScalar());

            if (tables == 0)
            {
                // Genuinely empty: a new environment, and every migration should run.
                return;
            }

            throw new InvalidOperationException(
                $"This database has {tables} table(s) but no migration history, so Entity " +
                $"Framework considers all {pendingCount} migration(s) pending and would try to " +
                "create objects that already exist. Refusing to start before touching anything. " +
                "Either this is the wrong database, or its __EFMigrationsHistory was lost and " +
                "has to be rebuilt to record the migrations the schema already contains.");
        }

        /// <summary>
        /// Serialises startup initialisation across instances with a SQL application lock.
        /// </summary>
        /// <remarks>
        /// Owned by the session rather than by a transaction: a migration opens and commits
        /// transactions of its own, and a lock that died with the first of them would be
        /// holding nothing for the rest of the work.
        /// </remarks>
        private void AcquireLock(SqlConnection gate)
        {
            var lockTimeout = ReadTimeout(LockTimeoutKey, DefaultLockTimeoutSeconds);

            using var command = gate.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "sp_getapplock";
            command.CommandTimeout = lockTimeout + 30;

            command.Parameters.AddWithValue("@Resource", LockResource);
            command.Parameters.AddWithValue("@LockMode", "Exclusive");
            command.Parameters.AddWithValue("@LockOwner", "Session");
            command.Parameters.AddWithValue("@LockTimeout", lockTimeout * 1000);

            var outcome = new SqlParameter
            {
                ParameterName = "@Result",
                SqlDbType = SqlDbType.Int,
                Direction = ParameterDirection.ReturnValue
            };
            command.Parameters.Add(outcome);

            command.ExecuteNonQuery();

            var code = Convert.ToInt32(outcome.Value ?? -999);

            // 0 granted immediately, 1 granted after waiting. Everything else is a refusal.
            if (code >= 0)
            {
                if (code == 1)
                {
                    _logger.LogInformation(
                        "Waited for another instance to finish initialising before taking the lock.");
                }

                return;
            }

            var reason = code switch
            {
                -1 => $"another instance held it for longer than {lockTimeout}s",
                -2 => "the request was cancelled",
                -3 => "it was chosen as a deadlock victim",
                _ => $"sp_getapplock returned {code}"
            };

            throw new InvalidOperationException(
                $"Could not acquire the startup lock '{LockResource}': {reason}. Refusing to " +
                "start: this instance cannot tell whether the database has the tables it was " +
                "compiled for, and starting anyway would serve trips against a schema nobody " +
                "has verified.");
        }

        private void ReleaseLock(SqlConnection gate)
        {
            try
            {
                using var command = gate.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_releaseapplock";
                command.Parameters.AddWithValue("@Resource", LockResource);
                command.Parameters.AddWithValue("@LockOwner", "Session");

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //
                // Deliberately not rethrown: this runs in a finally, and throwing here would
                // replace the real failure with this one. Closing the connection ends the
                // session, and a session-owned lock dies with it.
                //
                _logger.LogWarning(ex, "Could not release the startup lock explicitly.");
            }
        }

        /// <summary>
        /// A value outside this range is a typo, and a typo in a timeout is only discovered
        /// the day it matters. Clamped rather than thrown on: refusing to start over a badly
        /// typed timeout would be worse than the timeout.
        /// </summary>
        private int ReadTimeout(string key, int fallback)
        {
            var configured = _configuration.GetValue(key, fallback);

            if (configured is < 1 or > 3600)
            {
                _logger.LogWarning(
                    "{Key} is {Configured}, which is outside 1..3600 seconds. Using {Fallback}.",
                    key,
                    configured,
                    fallback);

                return fallback;
            }

            return configured;
        }

        /// <summary>
        /// Entity Framework wraps what the driver threw, so the chain is walked rather than
        /// the top of it inspected.
        /// </summary>
        private static bool IsTransient(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqlException { IsTransient: true })
                {
                    return true;
                }
            }

            return false;
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
