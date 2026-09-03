

using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Raphael.Api.Models;
using Raphael.Api.Services;
using Raphael.Api.Services.Admin;
using Raphael.Api.Services.Notifications;
using Raphael.Api.Services.Routing;
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
using Raphael.Shared.Time;
using Raphael.Shared.Validators;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Validate the whole dependency graph while building it, in every environment.
//
// By default this only happens in Development, so a service missing from the container
// does not stop the application: it throws the first time somebody resolves it, as a 500
// on whichever endpoint got there first. A missing delivery service once took down every
// controller that touches trips, schedules, riders or the bot, and from the outside it
// looked like a failure to load trips.
//
// Refusing to start is louder and far cheaper to diagnose than a service that answers
// some requests and not others.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

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
    //
    // ⚠️ Http + "bearer", not ApiKey. As an ApiKey scheme, Swagger sent the header exactly as
    // it was typed, so a token pasted on its own went out as `Authorization: eyJ...` with no
    // scheme, and JwtBearer — which only reads a header beginning with "Bearer " — answered
    // 401. It looked like the token was rejected. It was never read.
    //
    // As Http/bearer, Swagger writes the prefix itself and the box takes the bare token, which
    // is what anybody pastes anyway. The scheme name has to be lowercase: it is the OpenAPI
    // value, not the text of the header.
    //
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token on its own. Swagger adds the \"Bearer \" prefix."
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

    //
    // --- THE TWO API KEYS ---
    //
    // Three ways in, and they are not interchangeable. The JWT above is for the applications a
    // person signs into — Desktop, Driver, Rider. These two are machine to machine, each with
    // its own header and its own gate, and neither is ever accepted where the other is
    // expected. Declaring only one of them, as this used to, left the other undocumented and
    // impossible to try from here.
    //
    //   Authorization: Bearer …      JwtBearer + the global AuthorizeFilter
    //   X-Api-Key: …                 ApiKeyAuthFilter          → the Customer Service Bot
    //   X-Integration-ApiKey: …      IntegrationApiKeyAttribute → external integrators
    //
    // ⚠️ The requirements below are documentation, not enforcement: Swagger offers all three
    // and each endpoint is still guarded by whichever one its filter reads. Listing the three
    // is the honest description of an API where the answer is "one of these, depending on who
    // is calling".
    //
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Key of the Customer Service Bot. Sent as 'X-Api-Key'.",
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    options.AddSecurityDefinition("IntegrationApiKey", new OpenApiSecurityScheme
    {
        Description =
            "Key of an external integrator, the one on its Integrators row. Sent as " +
            "'X-Integration-ApiKey'. It also identifies who is calling: the trips it creates " +
            "and reads are that integrator's own.",
        In = ParameterLocation.Header,
        Name = "X-Integration-ApiKey",
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

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "IntegrationApiKey" },
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

    // ⚠️ Development only, and not merely for the per-query cost of both.
    // EnableSensitiveDataLogging writes parameter values into the logs, and the parameters of
    // this database are patient names, addresses and telephone numbers. Running it in
    // production is exactly the leak the constitution forbids in §3.
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});


builder.Services.AddResponseCompression(options =>
{
    // ⚠️ On for HTTPS, which is a deliberate call and not the framework default.
    //
    // The default is off because compressing a TLS response is what BREACH exploits. That
    // attack needs a secret and attacker-controlled text reflected in the same compressed
    // body; here the credential is a bearer token that travels in a header and is never
    // echoed back, and the bodies are trip data. Against that, every response this API sends
    // goes over the public internet to an office that pulls a whole operating day at a time.
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
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

//
// What time it is where the work happens.
//
// The timezone of the machine running this API is not a business input: a trip at 09:15
// is 09:15 at the pickup address, whoever is looking and wherever this is hosted. The
// zone comes from the provider carrying out the trip, falling back to the configured
// default — never to the host.
//
builder.Services.Configure<OperationTimeOptions>(
    builder.Configuration.GetSection(OperationTimeOptions.SectionName));

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IOperationClock, OperationClock>();

// ⚠️ Fails the deployment rather than the shift. A default timezone the host does not
// recognise would otherwise be discovered days later, as trips hours out of place.
var operationTimeZone = OperationClock.Resolve(
    builder.Configuration[
        $"{OperationTimeOptions.SectionName}:{nameof(OperationTimeOptions.DefaultTimeZone)}"]
    ?? new OperationTimeOptions().DefaultTimeZone);

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

//
// Routing: the one door to Google Maps.
//
// Every travel time, distance and coordinate in the ecosystem is bought here or served from
// the cache here. Desktop and Driver used to each call Google themselves, with the key on the
// machine and no memory between calls; a dialysis patient's leg was bought again by every
// dispatcher who looked at the route, at the traffic-aware rate, several thousand times a day.
//
// AddHttpClient rather than a static HttpClient: sockets get recycled, and the two clients get
// their own timeouts.
//
builder.Services.AddHttpClient<GoogleRoutesClient>();
builder.Services.AddHttpClient<GoogleGeocodingClient>();
builder.Services.AddScoped<IRoutingService, RoutingService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();

// Counts what we ask Google and what the cache answers, so the administrator's panel can show
// the bill and the saving instead of an opinion about them.
builder.Services.AddScoped<IMapsUsageService, MapsUsageService>();
builder.Services.AddScoped<IMapsUsageReportService, MapsUsageReportService>();
builder.Services.AddScoped<IObservedLegRecorder, ObservedLegRecorder>();

// Deletes cached Google answers past the retention the administrators set
// (Routing.CacheRetentionDays, default one year).
builder.Services.AddHostedService<RouteCachePurgeWorker>();

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

// Said out loud at startup so a misconfiguration is visible on the first line of the log
// rather than in a dispatcher's report three days later. If this hour does not match the
// clock on the office wall, nothing below it will be right.
#pragma warning disable RS0030 // The one legitimate read of the host clock: printing the
// gap between it and the operation is how a wrong setting becomes obvious at a glance.
app.Logger.LogInformation(
    "Operating timezone: {TimeZone}. It is {OperationTime} there now; the server's own clock " +
    "says {ServerTime}. If the first of those does not match the clock on the office wall, " +
    "nothing below this line will be right.",
    operationTimeZone.Id,
    TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, operationTimeZone),
    DateTime.Now);
#pragma warning restore RS0030

// Responses are compressed before anything else touches them. The dispatch office pulls a
// whole operating day — hundreds of rows of JSON — from a server that is not on the local
// network, so the wire is a real part of how long the Schedule tab takes to open.
app.UseResponseCompression();

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

