using Raphael.TripsService.Models;
using Microsoft.EntityFrameworkCore;

namespace Raphael.TripsService.Data
{
    public class TripsDbContext : DbContext
    {
        public TripsDbContext(DbContextOptions<TripsDbContext> options) : base(options) { }

        public DbSet<Trip> Trips { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}

