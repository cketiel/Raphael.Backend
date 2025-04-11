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
            // Asumimos que el archivo appsettings.json está en Meditrans.Shared
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  // Usamos la ruta de Meditrans.Shared
                .AddJsonFile("appsettings.json", optional: false) // Asegúrate de que el archivo esté en Meditrans.Shared
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<MediTransContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);  // Aquí se configura la conexión

            return new MediTransContext(optionsBuilder.Options);
        }
    }
}
