using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Raphael.Shared.DbContexts
{
    public class RaphaelContext : DbContext
    {
        public RaphaelContext(DbContextOptions<RaphaelContext> options) : base(options) { }

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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost;Database=RaphaelDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            
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
        }
    }
}

