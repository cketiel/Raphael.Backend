using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using Meditrans.Shared.DbContexts;

namespace Meditrans.Shared.Factories
{
    public class MediTransContextFactory : IDesignTimeDbContextFactory<MediTransContext>
    {
        public MediTransContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MediTransContext>();

            // Cargar configuración desde appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new MediTransContext(optionsBuilder.Options);
        }
    }
}
