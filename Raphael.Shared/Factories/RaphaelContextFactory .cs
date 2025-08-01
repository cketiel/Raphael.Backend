using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using Raphael.Shared.DbContexts;

namespace Raphael.Shared.Factories
{
    public class RaphaelContextFactory : IDesignTimeDbContextFactory<RaphaelContext>
    {
        public RaphaelContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RaphaelContext>();

            // Cargar configuración desde appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new RaphaelContext(optionsBuilder.Options);
        }
    }
}

