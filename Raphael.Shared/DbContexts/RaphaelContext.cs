using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Notifications;
using Raphael.Shared.Entities.Routing;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost;Database=RaphaelDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
    }
}

