

using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Raphael.Api.Models;
using Raphael.Api.Services;
using Raphael.Api.Services.Admin;
using Raphael.Api.Services.Notifications;
using Raphael.Api.Settings;
using Raphael.Notification.Application.DependencyInjection;
using Raphael.Notification.Infrastructure.DependencyInjection;
using Raphael.Notification.Infrastructure.Realtime.DependencyInjection;
using Raphael.Notification.Infrastructure.Realtime.Hubs;
using Raphael.Shared.Data;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Services;
using Raphael.Shared.Validators;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<CustomerCreateDto>, CustomerCreateDtoValidator>();

// SwaggerDoc
builder.Services.AddControllers();
builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure();
builder.Services.AddNotificationRealtime(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Raphael Backend API",
        Version = "v1"
    });

    // --- CONFIGURATION FOR JWT IN SWAGGER ---
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT token like this: Bearer {your_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Configure to use XML comments.
    // Raphael.Shared is included as well: the entities and DTOs that Swagger renders as
    // schemas are documented there, not here.
    foreach (var assemblyName in new[]
             {
                 Assembly.GetExecutingAssembly().GetName().Name,
                 typeof(Raphael.Shared.Entities.Trip).Assembly.GetName().Name
             })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");

        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access Bot endpoints. Example: 'X-Api-Key: YOUR_KEY'",
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
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

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
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
builder.Services.AddControllers(options =>
{
    // This applies the [Authorize] attribute to all controllers globally
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

//builder.Services.AddControllers();
/*builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });*/

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IIntegratorService, IntegratorService>();

builder.Services.AddScoped<IRatingService, RatingService>();

builder.Services.AddScoped<IRiderService, RiderService>();

builder.Services.AddScoped<BusinessEventCatalogSeeder>();
builder.Services.AddScoped<NotificationRuleCatalogSeeder>();
builder.Services.AddScoped<NotificationRuleService>();

// Register HttpClient for the Expo service
builder.Services.AddHttpClient<IExpoPushService, ExpoPushService>();

builder.Services.AddSingleton<IFirebaseMessagingService, FirebaseMessagingService>();
builder.Services.AddScoped<IDriverService, DriverService>();

// Single place where the payload of a trip event is assembled: which identifiers it
// carries is what decides who gets notified.
builder.Services.AddScoped<ITripNotificationPublisher, TripNotificationPublisher>();

// Integrations authenticate with an API Key, which must never travel in a URL. They
// exchange it for a short lived token that only opens the notification hub.
builder.Services.AddScoped<IIntegrationHubTokenService, IntegrationHubTokenService>();

// Nightly cleanup. Without it the notification tables only grow.
builder.Services.AddHostedService<NotificationRetentionWorker>();

// The notification module declares IDriverPushService and the API supplies it:
// Raphael.Notification cannot reference Raphael.Api, and the Firebase SDK admits
// a single default instance per process.
builder.Services.AddScoped<
    Raphael.Notification.Application.Interfaces.Delivery.IDriverPushService,
    FirebaseDriverPushService>();

// Map appsettings to the BotSettings class
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("BotSettings"));
// Register the security filter
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddScoped<IBotService, BotService>();

// Allow requests from the etamilanes.com domain
builder.Services.AddCors(options =>
{
    options.AddPolicy("EtamilanesPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://etamilanes.com",
                "https://www.etamilanes.com",
                "https://raphaeltransport.com",
                "https://www.raphaeltransport.com",
                "http://localhost:8081", // Expo Metro Bundler
                "http://localhost:19006"  // Web Expo
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
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


// Needs memory to store request counters
builder.Services.AddMemoryCache();

// Load Rate Limiting Settings
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// Inject the internal services of the library
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// Activate Rate Limiting
app.UseIpRateLimiting();

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

app.UseAuthentication(); // Who is the user?

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


app.UseAuthorization(); // Do you have permission?

app.MapHub<NotificationHub>("/hubs/notifications");

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

