using System.Collections.Generic;
using Raphael.UsersService.Models;
using Microsoft.EntityFrameworkCore;

namespace Raphael.UsersService.Data
{
    public class UsersDbContext : DbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

        //public DbSet<User> Users => Set<User>();

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Aquí puedes configurar el modelo, pero no es obligatorio si usas convenciones
        }
    }
}

