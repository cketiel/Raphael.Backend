using Meditrans.Shared.DbContexts;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Configure FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<MediTransContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
