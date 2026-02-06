

using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Raphael.Api.Services;
using Raphael.Api.Settings;
using Raphael.Shared.Data;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Validators;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<CustomerCreateDto>, CustomerCreateDtoValidator>();

// SwaggerDoc
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Raphael Backend API",
        Version = "v1"
    });
});

// Bind JwtSettings
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

builder.Services.AddAuthorization();

// Entity Framework DB
builder.Services.AddDbContext<RaphaelContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(); 
    options.EnableDetailedErrors();
});


// Add services to the container.

//builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Inject user services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();

// Trips
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<FundingSourceService>();
builder.Services.AddScoped<SpaceTypeService>();
builder.Services.AddScoped<CapacityTypeService>();
builder.Services.AddScoped<IFundingSourceBillingItemService, FundingSourceBillingItemService>();

// Vehicles
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<VehicleGroupService>();
builder.Services.AddScoped<ICapacityDetailTypeService, CapacityDetailTypeService>();
builder.Services.AddScoped<IRunService, RunService>();
builder.Services.AddScoped<IVehicleTypeService, VehicleTypeService>();

builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<BillingItemService>();
builder.Services.AddScoped<UnitService>();

builder.Services.AddScoped<IGpsService, GpsService>();

builder.Services.AddScoped<IProviderService, ProviderService>();

builder.Services.AddScoped<ITripHistoryService, TripHistoryService>();

// Allow requests from the etamilanes.com domain
builder.Services.AddCors(options =>
{
    options.AddPolicy("EtamilanesPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://etamilanes.com",
                "https://www.etamilanes.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Rate Limiting (ANTI-BOTS)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public-api", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 60; // 60 requests por IP
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 10;
    });
});



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Raphael Backend API v1");
});

// Swagger (optional)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.Environment.IsProduction
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();

app.UseDefaultFiles(); // So that it searches index.html if you access the root
app.UseStaticFiles();  // To serve files from wwwroot

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseCors("EtamilanesPolicy");
app.UseRateLimiter(); // Activate middleware Anti-bots  

// Apply Security Headers
// This protects against: clickjacking, sniffing, basic XSS
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'";
    await next();
});


app.UseAuthorization();

app.MapControllers();

// Initialize database (Apply migrations and initial data)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
        var initializer = services.GetRequiredService<IDbInitializer>();
        initializer.Initialize();
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred when executing the migration");
    }

}

// Middleware (Errors)
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    AllowStatusCode404Response = true,
    ExceptionHandler = async context =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exceptionHandler.Error, "Global exception handler caught error");

        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path
        });
    }
});

app.Run();

