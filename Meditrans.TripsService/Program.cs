using Meditrans.Shared.DbContexts;
using Meditrans.TripsService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Entity Framework DB
builder.Services.AddDbContext<MediTransContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MeditransConnection")));

/*var connectionString = builder.Configuration.GetConnectionString("TripsDb");
builder.Services.AddDbContext<TripsDbContext>(options =>
    options.UseSqlServer(connectionString));*/


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TripsDbContext>();
    Meditrans.TripsService.Data.DbInitializer.Seed(context);
}

app.Run();
