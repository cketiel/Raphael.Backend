using Raphael.Shared.DbContexts;
using Raphael.TripsService.Data;
using Raphael.TripsService.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<FundingSourceService>();
builder.Services.AddScoped<SpaceTypeService>();
builder.Services.AddScoped<CapacityTypeService>();

// Vehicle
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<VehicleGroupService>();
builder.Services.AddScoped<ICapacityDetailTypeService, CapacityDetailTypeService>();
builder.Services.AddScoped<IRunService, RunService>();




// Entity Framework DB
builder.Services.AddDbContext<RaphaelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaphaelConnection")));

/*var connectionString = builder.Configuration.GetConnectionString("TripsDb");
builder.Services.AddDbContext<TripsDbContext>(options =>
    options.UseSqlServer(connectionString));*/


/*builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
});*/


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Initialize database
/*using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TripsDbContext>();
    Raphael.TripsService.Data.DbInitializer.Seed(context);
}*/

app.Run();

