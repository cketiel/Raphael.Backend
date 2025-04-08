using Meditrans.TripsService.Models;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.TripsService.Data
{
    public class TripsDbContext : DbContext
    {
        public TripsDbContext(DbContextOptions<TripsDbContext> options) : base(options) { }

        public DbSet<Trip> Trips { get; set; }
    }
}
