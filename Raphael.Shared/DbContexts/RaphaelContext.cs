using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Raphael.Shared.Definitions.CallRequests;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.CallRequests;
using Raphael.Shared.Entities.Notifications;
using Raphael.Shared.Entities.Routing;
using Raphael.Shared.Routing;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Persistence.Configurations;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Shared.DbContexts
{
    public class RaphaelContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;
        //public RaphaelContext(DbContextOptions<RaphaelContext> options) : base(options) { }

        public RaphaelContext(DbContextOptions<RaphaelContext> options, ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Integrator> Integrators { get; set; }
        public DbSet<TripAttachment> TripAttachments { get; set; }
        public DbSet<TripHistory> TripHistories { get; set; }
        public DbSet<GPS> GPSData { get; set; }
        public DbSet<RouteSuspension> RouteSuspensions { get; set; }
        public DbSet<RouteAvailability> RouteAvailabilities { get; set; }
        public DbSet<RouteFundingSource> RouteFundingSources { get; set; }
        public DbSet<TripLog> TripLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<SpaceType> SpaceTypes { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleGroup> VehicleGroups { get; set; }
        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<CapacityType> Capacities { get; set; }
        public DbSet<CapacityDetail> CapacityDetails { get; set; }
        public DbSet<CapacityDetailType> CapacityDetailTypes { get; set; }
        public DbSet<VehicleRoute> VehicleRoutes { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<FundingSource> FundingSources { get; set; }
        public DbSet<BillingItem> BillingItems { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<FundingSourceBillingItem> FundingSourceBillingItems { get; set; }
        public DbSet<Schedule> Schedules { get; set; }

        #region Routing

        /// <summary>
        /// Google's answers, kept for as long as <c>Routing.CacheRetentionDays</c> says.
        /// See <see cref="RouteLegCacheEntry"/>.
        /// </summary>
        public DbSet<RouteLegCacheEntry> RouteLegCache { get; set; }

        /// <summary>Addresses resolved to coordinates, under the same retention setting.</summary>
        public DbSet<GeocodeCacheEntry> GeocodeCache { get; set; }

        /// <summary>What our own vehicles measured. No expiry — this one is ours.</summary>
        public DbSet<ObservedLegTime> ObservedLegTimes { get; set; }

        public DbSet<SystemSetting> SystemSettings { get; set; }

        /// <summary>Daily tallies of what we asked Google and what the cache answered.</summary>
        public DbSet<MapsUsageDaily> MapsUsageDaily { get; set; }

        /// <summary>Google's volume pricing, editable without a release.</summary>
        public DbSet<MapsPricingTier> MapsPricingTiers { get; set; }

        #endregion

        #region Notification Module

        public DbSet<NotificationModel> Notifications { get; set; }

        public DbSet<NotificationRecipient> NotificationRecipients { get; set; }

        public DbSet<NotificationDelivery> NotificationDeliveries { get; set; }

        public DbSet<NotificationMetadata> NotificationMetadata { get; set; }

        public DbSet<NotificationAction> NotificationActions { get; set; }
        public DbSet<BusinessEvent> BusinessEvents { get; set; }

        public DbSet<BusinessEventCategory> BusinessEventCategories { get; set; }
        public DbSet<BusinessEventDefinition> BusinessEventDefinitions { get; set; }

        public DbSet<BusinessEventGroup> BusinessEventGroups { get; set; }


        public DbSet<NotificationRule> NotificationRules { get; set; }

        public DbSet<NotificationRuleCondition> NotificationRuleConditions { get; set; }

        public DbSet<NotificationRuleRecipient> NotificationRuleRecipients { get; set; }

        public DbSet<NotificationRuleChannel> NotificationRuleChannels { get; set; }

        public DbSet<NotificationRuleAction> NotificationRuleActions { get; set; }

        /// <summary>
        /// Who archived, purged or deleted notification records. Outlives what it records.
        /// </summary>
        public DbSet<NotificationAdminAudit> NotificationAdminAudits { get; set; }

        #endregion

        #region Authentication

        /// <summary>
        /// Server-side sessions. Before this existed, signing out did nothing the server knew
        /// about and rotating the signing key was the only way to revoke anything.
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        #endregion

        #region Driver call requests

        public DbSet<DriverCallRequest> DriverCallRequests { get; set; }

        public DbSet<DriverCallRequestEvent> DriverCallRequestEvents { get; set; }

        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost;Database=RaphaelDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ======================================================
            // Authentication — server-side sessions
            // ======================================================

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(x => x.Id);

                // Base64 of a SHA-256 is always 44 characters. Bounded on purpose: an
                // unbounded string here would be an nvarchar(max) that cannot be indexed,
                // and every refresh is a lookup by this column.
                entity.Property(x => x.TokenHash)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(x => x.ReplacedByTokenHash)
                    .HasMaxLength(64);

                entity.Property(x => x.ClientApp)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.Property(x => x.RevokedReason)
                    .HasMaxLength(60);

                // The hot path: every refresh is "find the row for this hash". Unique because
                // two rows for one token would make reuse detection pick one at random.
                entity.HasIndex(x => x.TokenHash)
                    .IsUnique()
                    .HasDatabaseName("IX_RefreshTokens_TokenHash");

                // Reuse detection revokes a whole family at once, so the family is looked up
                // as a set.
                entity.HasIndex(x => x.FamilyId)
                    .HasDatabaseName("IX_RefreshTokens_FamilyId");

                // What the eventual cleanup job will sweep by, and what answers "is this
                // person still signed in anywhere".
                entity.HasIndex(x => x.AbsoluteExpiresAtUtc)
                    .HasDatabaseName("IX_RefreshTokens_AbsoluteExpiresAtUtc");

                // ⚠️ Two optional subjects, exactly one set per row -- staff sign in through
                // AuthController and are Users; patients identify through RiderController and
                // are Customers. Cascade on both: a person's sessions are not evidence and
                // have no reason to outlive them.
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Customer)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                // The database enforces "exactly one subject" rather than trusting every code
                // path to remember. A row with neither would be a session belonging to nobody;
                // a row with both would be a session belonging to two people.
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_RefreshTokens_OneSubject",
                    "([UserId] IS NOT NULL AND [CustomerId] IS NULL) OR ([UserId] IS NULL AND [CustomerId] IS NOT NULL)"));
            });

            // ======================================================
            // Driver call requests
            // ======================================================

            // ⚠️ No foreign keys to Users, VehicleRoutes or Schedules on purpose: a request is
            // history, and a constraint here would make deleting a route, a stop or a user fail
            // where it works today.
            modelBuilder.Entity<DriverCallRequest>(entity =>
            {
                entity.ToTable("DriverCallRequests");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.DriverName).IsRequired().HasMaxLength(150);
                entity.Property(x => x.RouteName).HasMaxLength(100);
                entity.Property(x => x.ClaimedByName).HasMaxLength(150);
                entity.Property(x => x.ResolvedByName).HasMaxLength(150);
                entity.Property(x => x.ReasonCode).HasMaxLength(30);
                entity.Property(x => x.ResolutionNote).HasMaxLength(CallRequestReasonCodes.NoteMaxLength);
                entity.Property(x => x.OperatingDate).HasColumnType("date");
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.Ignore(x => x.IsOpen);

                // One open case per driver, enforced by the database: two taps racing each
                // other cannot open two cases; the loser fails here and becomes a reminder.
                entity.HasIndex(x => x.DriverId)
                    .IsUnique()
                    .HasFilter("[Status] IN (0, 1)")
                    .HasDatabaseName("IX_DriverCallRequests_Driver_Open");

                entity.HasIndex(x => new { x.Status, x.QueuedAtUtc })
                    .HasDatabaseName("IX_DriverCallRequests_Status_Queue");

                entity.HasIndex(x => new { x.OperatingDate, x.DriverId })
                    .HasDatabaseName("IX_DriverCallRequests_Day_Driver");

                entity.HasMany(x => x.Events)
                    .WithOne(e => e.CallRequest)
                    .HasForeignKey(e => e.CallRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DriverCallRequestEvent>(entity =>
            {
                entity.ToTable("DriverCallRequestEvents");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ByName).HasMaxLength(150);
                entity.Property(x => x.Detail).HasMaxLength(150);

                entity.HasIndex(x => new { x.CallRequestId, x.AtUtc })
                    .HasDatabaseName("IX_DriverCallRequestEvents_Request_At");
            });

            // ⚠️ No relationship to Notifications on purpose. This table exists to outlive
            // the rows it describes; a cascade from a deleted notification would erase the
            // evidence of its own deletion.
            modelBuilder.Entity<NotificationAdminAudit>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Action)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.Property(x => x.PerformedByUsername)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Details)
                    .HasMaxLength(500);

                // The panel reads it newest first, and always the whole trail.
                entity.HasIndex(x => x.PerformedAtUtc);
            });

            // ======================================================
            // Routing cache and observed times
            // ======================================================

            modelBuilder.Entity<RouteLegCacheEntry>(entity =>
            {
                entity.ToTable("RouteLegCache");
                entity.HasKey(x => x.Id);

                // The whole key, and unique: two rows for the same leg under the same
                // conditions would mean paying for it twice and then choosing at random.
                entity.HasIndex(x => new
                {
                    x.OriginLatE4,
                    x.OriginLngE4,
                    x.DestLatE4,
                    x.DestLngE4,
                    x.TimeBucket,
                    x.DayType,
                    x.TrafficMode
                })
                .IsUnique()
                .HasDatabaseName("IX_RouteLegCache_Leg");

                // An encoded polyline for a city drive runs to a few kilobytes, and a long one
                // has no useful ceiling. Left unbounded rather than truncated: half a polyline
                // draws a road that ends in a field.
                entity.Property(x => x.EncodedPolyline);

                // The purge reads by age and nothing else.
                entity.HasIndex(x => x.FetchedAtUtc);
            });

            modelBuilder.Entity<GeocodeCacheEntry>(entity =>
            {
                entity.ToTable("GeocodeCache");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.NormalizedAddress)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(x => x.PlaceId).HasMaxLength(128);
                entity.Property(x => x.FormattedAddress).HasMaxLength(300);

                entity.HasIndex(x => x.NormalizedAddress)
                    .IsUnique()
                    .HasDatabaseName("IX_GeocodeCache_Address");

                entity.HasIndex(x => x.FetchedAtUtc);
            });

            // ⚠️ No foreign key to Schedule or VehicleRoute on purpose. These rows outlive the
            // schedules that produced them: a route deleted at the end of a contract must not
            // erase a year of measured travel times, which is the only data the automatic
            // router will have to learn from.
            modelBuilder.Entity<ObservedLegTime>(entity =>
            {
                entity.ToTable("ObservedLegTimes");
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new
                {
                    x.OriginLatE4,
                    x.OriginLngE4,
                    x.DestLatE4,
                    x.DestLngE4,
                    x.DayType,
                    x.TimeBucket
                })
                .HasDatabaseName("IX_ObservedLegTimes_Leg");

                entity.HasIndex(x => x.ObservedAtUtc);
            });

            modelBuilder.Entity<MapsUsageDaily>(entity =>
            {
                entity.ToTable("MapsUsageDaily");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Day).HasColumnType("date");

                // One row per day, product and outcome, and the counter is incremented in place.
                // The unique index is what lets the increment be an upsert instead of a read
                // followed by a write that two dispatchers can interleave.
                entity.HasIndex(x => new { x.Day, x.Sku, x.Billed })
                    .IsUnique()
                    .HasDatabaseName("IX_MapsUsageDaily_Day_Sku_Billed");
            });

            modelBuilder.Entity<MapsPricingTier>(entity =>
            {
                entity.ToTable("MapsPricingTiers");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.PricePerThousand).HasColumnType("decimal(10,4)");

                entity.HasIndex(x => new { x.Sku, x.FromRequest })
                    .IsUnique()
                    .HasDatabaseName("IX_MapsPricingTiers_Sku_From");

                // Google's published prices as of August 2026, seeded so the panel can cost a
                // period the day it is deployed. They live in the database precisely so the next
                // price change is an UPDATE rather than a release — see MapsPricingTier.
                //
                // ⚠️ The band boundaries are shared across products (100k, 500k, 1M, 5M) but the
                // free allowance is not: Routes Pro gives 5,000 where the rest give 10,000, so its
                // first paid band starts at 5,001 while theirs start at 10,001. Google's own
                // pricing page prints the first band as "10,001–100,000" for every product, which
                // cannot be right for a product whose free tier ends at 5,000. Read here as
                // "charging begins where the free allowance ends" — the reading that costs the
                // business money rather than surprises it. Correct the row if an invoice disagrees.
                entity.HasData(
                    Tier(1, MapsSku.RoutesEssentials, 10_000, 10_001, 100_000, 5.00m),
                    Tier(2, MapsSku.RoutesEssentials, 10_000, 100_001, 500_000, 4.00m),
                    Tier(3, MapsSku.RoutesEssentials, 10_000, 500_001, 1_000_000, 3.00m),
                    Tier(4, MapsSku.RoutesEssentials, 10_000, 1_000_001, 5_000_000, 1.50m),
                    Tier(5, MapsSku.RoutesEssentials, 10_000, 5_000_001, null, 0.38m),

                    Tier(6, MapsSku.RoutesPro, 5_000, 5_001, 100_000, 10.00m),
                    Tier(7, MapsSku.RoutesPro, 5_000, 100_001, 500_000, 8.00m),
                    Tier(8, MapsSku.RoutesPro, 5_000, 500_001, 1_000_000, 6.00m),
                    Tier(9, MapsSku.RoutesPro, 5_000, 1_000_001, 5_000_000, 3.00m),
                    Tier(10, MapsSku.RoutesPro, 5_000, 5_000_001, null, 0.75m),

                    Tier(11, MapsSku.Geocoding, 10_000, 10_001, 100_000, 5.00m),
                    Tier(12, MapsSku.Geocoding, 10_000, 100_001, 500_000, 4.00m),
                    Tier(13, MapsSku.Geocoding, 10_000, 500_001, 1_000_000, 3.00m),
                    Tier(14, MapsSku.Geocoding, 10_000, 1_000_001, 5_000_000, 1.50m),
                    Tier(15, MapsSku.Geocoding, 10_000, 5_000_001, null, 0.38m),

                    Tier(16, MapsSku.DynamicMaps, 10_000, 10_001, 100_000, 7.00m),
                    Tier(17, MapsSku.DynamicMaps, 10_000, 100_001, 500_000, 5.60m),
                    Tier(18, MapsSku.DynamicMaps, 10_000, 500_001, 1_000_000, 4.20m),
                    Tier(19, MapsSku.DynamicMaps, 10_000, 1_000_001, 5_000_000, 2.10m),
                    Tier(20, MapsSku.DynamicMaps, 10_000, 5_000_001, null, 0.53m),

                    Tier(21, MapsSku.PlacesAutocomplete, 10_000, 10_001, 100_000, 2.83m),
                    Tier(22, MapsSku.PlacesAutocomplete, 10_000, 100_001, 500_000, 2.27m),
                    Tier(23, MapsSku.PlacesAutocomplete, 10_000, 500_001, 1_000_000, 1.70m),
                    Tier(24, MapsSku.PlacesAutocomplete, 10_000, 1_000_001, 5_000_000, 0.85m),
                    Tier(25, MapsSku.PlacesAutocomplete, 10_000, 5_000_001, null, 0.21m),

                    Tier(26, MapsSku.PlaceDetails, 10_000, 10_001, 100_000, 5.00m),
                    Tier(27, MapsSku.PlaceDetails, 10_000, 100_001, 500_000, 4.00m),
                    Tier(28, MapsSku.PlaceDetails, 10_000, 500_001, 1_000_000, 3.00m),
                    Tier(29, MapsSku.PlaceDetails, 10_000, 1_000_001, 5_000_000, 1.50m),
                    Tier(30, MapsSku.PlaceDetails, 10_000, 5_000_001, null, 0.38m));
            });

            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.ToTable("SystemSettings");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Key).IsRequired().HasMaxLength(100);
                entity.Property(x => x.Value).IsRequired().HasMaxLength(400);
                entity.Property(x => x.Description).HasMaxLength(400);
                entity.Property(x => x.UpdatedBy).HasMaxLength(100);

                entity.HasIndex(x => x.Key).IsUnique();
            });

            modelBuilder.Entity<Integrator>()
                .HasOne(i => i.FundingSource)
                .WithMany() // A FundingSource can be associated with multiple integrators.
                .HasForeignKey(i => i.FundingSourceId)
                .OnDelete(DeleteBehavior.Restrict); // Do not delete FundingSource if the Integrator is deleted.

            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasOne(r => r.Trip)
                    .WithMany()
                    .HasForeignKey(r => r.TripId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes if a Trip is deleted

                entity.HasOne(r => r.Customer)
                    .WithMany()
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Driver)
                    .WithMany()
                    .HasForeignKey(r => r.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VehicleRoute>()
                .HasMany(v => v.Suspensions)
                .WithOne(s => s.VehicleRoute)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleRoute>()
                .HasMany(v => v.Availabilities)
                .WithOne(a => a.VehicleRoute)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleRoute>()
                .HasMany(v => v.FundingSources)
                .WithOne(f => f.VehicleRoute)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteFundingSource>()
            .HasKey(rfs => new { rfs.VehicleRouteId, rfs.FundingSourceId });

            modelBuilder.Entity<RouteFundingSource>()
                .HasOne(rfs => rfs.VehicleRoute)
                .WithMany(vr => vr.FundingSources)
                .HasForeignKey(rfs => rfs.VehicleRouteId);

            modelBuilder.Entity<RouteFundingSource>()
                .HasOne(rfs => rfs.FundingSource)
                .WithMany()
                .HasForeignKey(rfs => rfs.FundingSourceId);

            modelBuilder.Entity<VehicleRoute>()
            .Property(v => v.FromTime)
            .HasColumnType("time");

            modelBuilder.Entity<VehicleRoute>()
                .Property(v => v.ToTime)
                .HasColumnType("time");

            modelBuilder.Entity<RouteAvailability>()
                .Property(ra => ra.StartTime)
                .HasColumnType("time");

            modelBuilder.Entity<RouteAvailability>()
                .Property(ra => ra.EndTime)
                .HasColumnType("time");

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.RiderId)
                .IsUnique();

            modelBuilder.Entity<SpaceType>()
                .HasIndex(st => st.Name)
                .IsUnique();

            modelBuilder.Entity<TripLog>()
                .HasOne(tl => tl.Trip)
                .WithMany(t => t.TripLogs)
                .HasForeignKey(tl => tl.TripId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Customer)
                .WithMany(c => c.Trips)
                .HasForeignKey(t => t.CustomerId);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.SpaceType)
                .WithMany(s => s.Trips)
                .HasForeignKey(t => t.SpaceTypeId);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.VehicleGroup)
                .WithMany()
                .HasForeignKey(v => v.GroupId);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.VehicleType)
                .WithMany()
                .HasForeignKey(v => v.VehicleTypeId);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.CapacityDetailType)
                .WithMany()
                .HasForeignKey(v => v.CapacityDetailTypeId);
                //.WithMany(c => c.Vehicles)
                //.HasForeignKey(v => v.CapacityDetailTypeId);

            modelBuilder.Entity<CapacityDetail>()
                .HasOne(cd => cd.SpaceType)
                .WithMany(st => st.CapacityDetails)
                .HasForeignKey(cd => cd.SpaceTypeId);


            modelBuilder.Entity<VehicleRoute>()
                .HasOne(vr => vr.Driver)
                .WithMany()
                //.WithMany(u => u.VehicleRoutes)
                .HasForeignKey(vr => vr.DriverId);

            modelBuilder.Entity<VehicleRoute>()
                .HasOne(vr => vr.Vehicle);
                //.WithMany(v => v.VehicleRoutes)
                //.HasForeignKey(vr => vr.VehicleId);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.FundingSource)
                //.WithMany(fs => fs.Customers)
                .WithMany()
                .HasForeignKey(c => c.FundingSourceId);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.SpaceType)
                //.WithMany(st => st.Customers)
                .WithMany()
                .HasForeignKey(c => c.SpaceTypeId);

            modelBuilder.Entity<BillingItem>()
                .HasOne(b => b.Unit)
                //.WithMany(u => u.BillingItems)
                .WithMany()
                .HasForeignKey(b => b.UnitId);

            modelBuilder.Entity<FundingSourceBillingItem>()
                .HasOne(fsbi => fsbi.FundingSource)
                .WithMany(fs => fs.BillingItems)
                .HasForeignKey(fsbi => fsbi.FundingSourceId);

            modelBuilder.Entity<FundingSourceBillingItem>()
                .HasOne(fsbi => fsbi.BillingItem)
                .WithMany(bi => bi.FundingSourceBillingItems)
                .HasForeignKey(fsbi => fsbi.BillingItemId);

            modelBuilder.Entity<FundingSourceBillingItem>()
                .HasOne(fsbi => fsbi.SpaceType)
                .WithMany(st => st.FundingSourceBillingItems)
                .HasForeignKey(fsbi => fsbi.SpaceTypeId);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Trip)
                .WithMany()
                //.WithMany(t => t.Schedules)
                .HasForeignKey(s => s.TripId);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.VehicleRoute)
                .WithMany(vr => vr.Schedules)
                .HasForeignKey(s => s.VehicleRouteId);

            // ===== Indexes for the dispatcher's Schedule screen =====
            // Until these existed, Schedules carried nothing but the two foreign-key indexes
            // EF creates by convention. Every read of "this route, this day" seeked on
            // VehicleRouteId and then scanned what came back looking at Date — for a route with
            // a year of history, that is a year of rows read to show one morning.

            // The screen's main query, and the one behind every routing and every reorder.
            // Sequence is included rather than keyed: it is what the results are ordered by,
            // and carrying it in the leaf spares the sort.
            modelBuilder.Entity<Schedule>()
                .HasIndex(s => new { s.VehicleRouteId, s.Date })
                .IncludeProperties(s => s.Sequence)
                .HasDatabaseName("IX_Schedules_Route_Date");

            // The driver-facing reads filter by the route's smartphone login, which is a column
            // on the joined table and was unindexed on both sides of the join.
            modelBuilder.Entity<VehicleRoute>()
                .HasIndex(vr => vr.SmartphoneLogin)
                .HasDatabaseName("IX_VehicleRoutes_SmartphoneLogin");

            // "Where is this vehicle now" — asked for every route on screen, and answered by
            // ordering that route's whole GPS history by time and taking the first row.
            modelBuilder.Entity<GPS>()
                .HasIndex(g => new { g.IdVehicleRoute, g.DateTime })
                .HasDatabaseName("IX_GPSData_Route_DateTime");

            // The backlog query: the trips of one day that have no route yet. The unique index
            // on Trip leads with Date, but it is filtered on IsCancelled and cannot serve a
            // predicate on VehicleRouteId being null.
            modelBuilder.Entity<Trip>()
                .HasIndex(t => new { t.Date, t.VehicleRouteId })
                .HasFilter("[IsCancelled] = 0")
                .HasDatabaseName("IX_Trips_Date_Route_Active");

            // To avoid duplicate trips
            // Define maximum sizes for the columns that will be part of the index
            // SQL Server has a 900 byte limit for index keys. Since we use nvarchar (2 bytes per character), 450 is the safe maximum.
            modelBuilder.Entity<Trip>()
                .Property(t => t.PickupAddress)
                .HasMaxLength(450)
                .IsRequired();

            modelBuilder.Entity<Trip>()
                .Property(t => t.DropoffAddress)
                .HasMaxLength(450)
                .IsRequired();

            // Create the Unique Filtered Index
            modelBuilder.Entity<Trip>()
                .HasIndex(t => new {
                    t.Date,
                    t.CustomerId,
                    t.PickupAddress,
                    t.DropoffAddress,
                    t.FromTime,
                    t.ToTime
                })
                .IsUnique()
                .HasFilter("[IsCancelled] = 0") // Only apply the uniqueness rule if it is NOT canceled
                .HasDatabaseName("IX_Trip_Unique_Active_Trip");

            modelBuilder.Entity<Trip>().HasQueryFilter(t =>
                _currentUserService.IsMilanesInternal ||
                (_currentUserService.IntegratorId != null && t.IntegratorId == _currentUserService.IntegratorId) ||
                (_currentUserService.ProviderId != null && t.ProviderId == _currentUserService.ProviderId)
            );

            modelBuilder.Entity<Customer>().HasQueryFilter(c =>
               _currentUserService.IsMilanesInternal ||
               (_currentUserService.IntegratorId != null && c.IntegratorId == _currentUserService.IntegratorId) ||
               (_currentUserService.ProviderId != null)
           );

            #region Notification Module

            // ======================================================
            // Notification Aggregate
            // ======================================================

            modelBuilder.Entity<NotificationModel>()
                .HasMany(n => n.Recipients)
                .WithOne(r => r.Notification)
                .HasForeignKey(r => r.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<NotificationModel>()
                .HasMany(n => n.Deliveries)
                .WithOne(d => d.Notification)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<NotificationModel>()
                .HasMany(n => n.Metadata)
                .WithOne(m => m.Notification)
                .HasForeignKey(m => m.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<NotificationModel>()
                .HasMany(n => n.Actions)
                .WithOne(a => a.Notification)
                .HasForeignKey(a => a.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);


            // ======================================================
            // Notification Rules
            // ======================================================

            modelBuilder.Entity<NotificationRule>()
                .HasOne(nr => nr.BusinessEventDefinition)
                .WithMany()
                .HasForeignKey(nr => nr.BusinessEventDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<NotificationRuleCondition>()
                .HasOne(c => c.NotificationRule)
                .WithMany(r => r.Conditions)
                .HasForeignKey(c => c.NotificationRuleId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<NotificationRuleRecipient>()
                .HasOne(r => r.NotificationRule)
                .WithMany(nr => nr.Recipients)
                .HasForeignKey(r => r.NotificationRuleId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<NotificationRuleChannel>()
                .HasOne(c => c.NotificationRule)
                .WithMany(nr => nr.Channels)
                .HasForeignKey(c => c.NotificationRuleId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<NotificationRuleAction>()
                .HasOne(a => a.NotificationRule)
                .WithMany(nr => nr.Actions)
                .HasForeignKey(a => a.NotificationRuleId)
                .OnDelete(DeleteBehavior.Cascade);


            // ======================================================
            // Business Event Hierarchy
            // ======================================================

            // BusinessEventCategory -> BusinessEventGroup

            modelBuilder.Entity<BusinessEventGroup>()
                .HasOne(bg => bg.Category)
                .WithMany()
                .HasForeignKey(bg => bg.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // BusinessEventGroup -> BusinessEvent

            modelBuilder.Entity<BusinessEvent>()
                .HasOne(be => be.Group)
                .WithMany()
                .HasForeignKey(be => be.GroupId)
                .OnDelete(DeleteBehavior.Restrict);


            // BusinessEvent -> BusinessEventDefinition

            modelBuilder.Entity<BusinessEventDefinition>()
                .HasOne(bed => bed.BusinessEvent)
                .WithMany()
                .HasForeignKey(bed => bed.BusinessEventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusinessEventCategory>()
                .ToTable("BusinessEventCategory");


            #endregion
        }

        /// <summary>One seeded pricing band. Keeps the table above readable as a table.</summary>
        private static MapsPricingTier Tier(
            int id,
            MapsSku sku,
            int freeCap,
            int from,
            int? to,
            decimal pricePerThousand) => new()
            {
                Id = id,
                Sku = sku,
                FreeCapPerMonth = freeCap,
                FromRequest = from,
                ToRequest = to,
                PricePerThousand = pricePerThousand
            };
    }
}

