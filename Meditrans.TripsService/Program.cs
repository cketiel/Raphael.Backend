using Meditrans.TripsService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


var connectionString = builder.Configuration.GetConnectionString("TripsDb");

builder.Services.AddDbContext<TripsDbContext>(options =>
    options.UseSqlServer(connectionString));


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
