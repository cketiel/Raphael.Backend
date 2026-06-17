using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Interfaces;
using System.IO;

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

            var dummyUserService = new DesignTimeCurrentUserService();

            return new RaphaelContext(optionsBuilder.Options, dummyUserService);
        }
    }

    // Clase auxiliar interna para que la fábrica funcione
    // En tiempo de diseño (migraciones), asumimos que somos Milanes (Admin)
    public class DesignTimeCurrentUserService : ICurrentUserService
    {
        public int? UserId => null;
        public int? IntegratorId => null;
        public int? ProviderId => null;
        public bool IsMilanesInternal => true; // Para migraciones, actuar como admin global
    }
}

