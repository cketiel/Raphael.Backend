

using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Raphael.Api.Models;
using Raphael.Api.Observability;
using Raphael.Api.Realtime;
using Raphael.Api.Services;
using Raphael.Api.Services.Admin;
using Raphael.Api.Services.Notifications;
using Raphael.Api.Services.Routing;
using Raphael.Api.Settings;
using Raphael.Api.Versioning;
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
    // ⚠️ This "v1" is the name of the OpenAPI document, not the version of the build serving
    // it. They are different numbers that move for different reasons, and reading this one as
    // "the API is at version 1" is how the question "which version is in production?" went a
    // year without an answer. The build number is at GET /api/version; see ApiContract.
    options.SwaggerDoc(ApiContract.Document, new OpenApiInfo
    {
        Title = "Raphael Backend API",
        Version = ApiContract.Document
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

//
// ⚠️ Fails the deployment rather than the shift, the same as an empty CORS list and an
// unrecognised timezone below.
//
// This exists so the signing key can be taken OUT of the committed appsettings.json. While a
// working key sat in that file, a deployment that failed to supply its own silently inherited
// the one published in a public repository — the worst possible failure, because everything
// works and every token the API issues can be forged by anyone who read the file.
//
// Without this guard the symptom of a missing key is an ArgumentNullException from
// Encoding.UTF8.GetBytes, thrown from inside the authentication builder, which reads as a
// framework fault rather than a configuration one.
//
// Where it comes from: Key Vault via the app setting Jwt__Key in Azure, and User Secrets on a
// developer's machine (dotnet user-secrets set "Jwt:Key" "<a long random value>").
//
if (string.IsNullOrWhiteSpace(jwtSettings?.Key))
{
    throw new InvalidOperationException(
        "'Jwt:Key' is empty. It is the key this API signs and validates every token with, and " +
        "it is deliberately not in any committed file. Supply it from Key Vault (app setting " +
        "'Jwt__Key') or, on a development machine, from User Secrets. Refusing to start: an " +
        "API that cannot verify a token cannot tell a dispatcher from a stranger.");
}

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

            // SignalR can only put its credential in the query string when the transport is
            // not WebSockets, and this host may fall back to one of those. Both hubs need the
            // same accommodation.
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/notifications") ||
                 path.StartsWithSegments("/hubs/dispatch")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

//
// Whether this API may write the database's own parameter values into its logs.
//
// ⚠️ Those parameters are patient names, addresses, telephone numbers and signatures, and §3
// of the constitution forbids them in logs and in URLs.
//
// This used to be switched on by the environment being called "Development", which tied a
// safety property to a label. The Azure environment is called Development for entirely
// ordinary reasons — it is the one that is not production — and it is about to receive a
// restore of the real database. Nobody chose that combination; it arrived by name.
//
// So it is its own key now, and it is off unless somebody writes it down. No environment can
// acquire it by being called something. Turn it on deliberately, against data you are willing
// to read in a log, and turn it off when the diagnosis is over.
//
const string LogSensitiveDataKey = "Diagnostics:LogSensitiveData";
var logSensitiveData = builder.Configuration.GetValue<bool>(LogSensitiveDataKey);

//
// ⚠️ Fails the deployment rather than the shift, like the signing key above.
//
// This exists so the connection string can be taken OUT of the committed appsettings.json,
// where it sat holding the credentials of the live production database in a public
// repository. The guard is what makes removing it safe: without one, an environment that
// failed to supply its own would have quietly inherited the published credentials and worked
// perfectly, which is the failure nobody notices.
//
// Where it comes from: Key Vault via ConnectionStrings__DefaultConnection in Azure, the
// server's own appsettings.Production.json on MyASP.NET, and appsettings.Development.json —
// which points at localhost with Windows authentication and is therefore not a secret.
//
const string ConnectionStringKey = "DefaultConnection";
var connectionString = builder.Configuration.GetConnectionString(ConnectionStringKey);

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"'ConnectionStrings:{ConnectionStringKey}' is empty. It is deliberately not in any " +
        "committed file. Supply it from Key Vault (app setting " +
        "'ConnectionStrings__DefaultConnection'), from the server's own " +
        "appsettings.Production.json, or from appsettings.Development.json for a local " +
        "database. Refusing to start: there is no database to serve trips from.");
}

// Entity Framework DB
builder.Services.AddDbContext<RaphaelContext>(options =>
{
    options.UseSqlServer(connectionString);

    if (logSensitiveData)
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

// The dispatch board channel. Not a notification: see Raphael.Api/Realtime/DispatchHub.cs
// and _meta/REALTIME_POLICY.md for why it is a hub of its own.
builder.Services.AddScoped<IDispatchBroadcaster, DispatchBroadcaster>();

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

// Named after what it governs, not after one of the sites it lets in. The old name,
// "EtamilanesPolicy", described the first client anybody added and then outlived it.
const string CorsPolicyName = "BrowserClients";
const string CorsOriginsKey = "Cors:AllowedOrigins";

//
// Which browsers may call this API.
//
// The list used to be six literals compiled into this file, which meant a new front end — or
// the same front end on a new host — needed a rebuild and a redeploy of the whole API to be
// allowed to talk to it. It is configuration now because the API is about to exist on two
// hosts at once: MyASP.NET keeps serving the origins it has always served while Azure comes up
// under its own domain, and neither list is the other's.
//
// Only browsers are governed by this. Desktop and Driver send no Origin header and are
// unaffected; the ones that are affected are the ETA page, the booking portal and Expo on the
// web.
//
var corsOrigins = builder.Configuration.GetSection(CorsOriginsKey).Get<string[]>()
                  ?? Array.Empty<string>();

// ⚠️ Fails the deployment rather than the shift, the same as an unrecognised timezone below.
// AllowCredentials makes "*" illegal, so an empty list is not "everybody": it is nobody, and
// what the browser shows is a CORS error that names no cause. Outside Development, a
// configuration that did not arrive stops the app here, where the reason is written down.
if (corsOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        $"'{CorsOriginsKey}' is empty. It must list the browser origins allowed to call this " +
        "API, scheme and host, no trailing slash. Refusing to start: an empty list authorises " +
        "nobody and surfaces hours later as an unexplained CORS failure in somebody's browser.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()

            // A browser hands JavaScript only the handful of response headers it is told to,
            // and these two are the ones a page is meant to read. Without this the rider app
            // running on the web is told nothing when its version is out of date -- the header
            // arrives, the browser drops it, and the notice never appears. Desktop and Driver
            // are unaffected either way: no browser sits between them and this API.
            .WithExposedHeaders(
                ClientVersionHeaders.Status,
                ClientVersionHeaders.Minimum);
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

//
// What the request looked like before the reverse proxy touched it.
//
// App Service terminates TLS at its front end and hands the request to the container over
// plain HTTP, on a private address. Two things in this pipeline read the wrong value without
// this, and both fail in ways that do not look like a proxy problem:
//
//   · UseHttpsRedirection sees scheme "http" and answers 307 to the very same URL, which the
//     front end forwards again as http. That is an infinite redirect, not an error message.
//   · The IP rate limiter sees the front end's address for every caller on earth, so the five
//     logins a minute allowed per IP become five logins a minute for the whole company.
//
// ⚠️ KnownNetworks and KnownProxies are cleared on purpose. By default the middleware only
// trusts a forwarder on loopback, and Azure's front end is neither loopback nor an address
// known in advance, so the defaults make it drop the headers in silence — indistinguishable
// from never having added it. Clearing them is safe here because nothing reaches this
// container except through that front end.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

//
// Telemetry.
//
// Reads APPLICATIONINSIGHTS_CONNECTION_STRING from configuration, which the Azure environment
// sets and MyASP.NET does not. Absent, the SDK collects nothing and the app runs exactly as it
// does today — which is what lets the same build serve both hosts during the migration.
//
// ⚠️ The initializer is not decoration. Without it this sends bearer tokens and patient
// addresses to a store outside the building; see QueryStringScrubbingInitializer.
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer, QueryStringScrubbingInitializer>();

// Which application, and which of its versions, made the call. The SDK stamps the other half —
// it reads the assembly version this build declares into application_Version on its own, which
// is why Directory.Build.props declares one. With both, "the import failed on Tuesday" is a
// query and not an interrogation. See ClientVersionTelemetryInitializer.
builder.Services.AddSingleton<ITelemetryInitializer, ClientVersionTelemetryInitializer>();

//
// The oldest version of each client application this build expects to serve.
//
// Configuration rather than a constant because the floor rises the day a release is actually
// distributed to the people who use it, which is never the day the server code was written.
// Empty is a valid answer and the one it ships with: nobody is reported as outdated until
// somebody decides what current means.
//
builder.Services.Configure<ClientCompatibilityOptions>(
    builder.Configuration.GetSection(ClientCompatibilityOptions.SectionName));

// And the health probe, which is a minute-by-minute heartbeat rather than traffic, does not
// need to be stored 43,000 times a month to be believed. Only while it succeeds: see
// HealthProbeTelemetryProcessor.
builder.Services.AddApplicationInsightsTelemetryProcessor<HealthProbeTelemetryProcessor>();

//
// Health.
//
// Two endpoints, because the question "is this process alive" and the question "can this
// process do its job" have different consequences when the answer is no. App Service probes
// the first and restarts what fails it; a human reads the second. See DatabaseHealthCheck.
//
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        "database",
        HealthStatus.Unhealthy,
        tags: new[] { DatabaseHealthCheck.ReadyTag });

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

// Said out loud for the same reason as the line above it: a setting this dangerous should not
// be discoverable only by reading the configuration of a running app. If this appears in a log
// that anybody but the person diagnosing can read, it has already gone on too long.
if (logSensitiveData)
{
    app.Logger.LogWarning(
        "{Key} is ON. SQL parameter values — patient names, addresses, telephone numbers — are " +
        "being written to these logs. This is a diagnostic switch, not a setting: turn it off.",
        LogSensitiveDataKey);
}

// First, before anything reads the scheme or the caller's address. Everything below this line
// — the redirect to HTTPS, the per-IP rate limiter, the telemetry — would otherwise be
// describing the reverse proxy instead of the client. See ForwardedHeadersOptions above.
app.UseForwardedHeaders();

// Who is calling, and how old they are. Placed here, above the rate limiter, so that a call
// rejected with a 429 is still attributed to the application that made it — a burst from one
// version of Raphael.Desktop is precisely the kind of thing worth being able to see.
app.UseMiddleware<ClientCompatibilityMiddleware>();

// Responses are compressed before anything else touches them. The dispatch office pulls a
// whole operating day — hundreds of rows of JSON — from a server that is not on the local
// network, so the wire is a real part of how long the Schedule tab takes to open.
app.UseResponseCompression();

// Activate Rate Limiting
app.UseIpRateLimiting();

//
// Swagger, everywhere except production.
//
// ⚠️ This used to be registered three times — once unconditionally, once inside a check for
// Development that therefore did nothing, and once more under a comment that said
// "IsProduction". The effect was that the document and its UI were served to anybody who asked,
// in every environment.
//
// That is being changed the week production starts holding real patient data. Swagger publishes
// the whole shape of the system: 31 controllers, every DTO, and the existence and header name of
// both machine-to-machine keys. None of it is PHI, and publishing it is not a breach — it is the
// map somebody would otherwise have to guess at, handed over anonymously, on a host that from
// this week is inside the same perimeter as production (CLAUDE.md §3).
//
// What is lost, and where it went:
//
//   · Integrators explore against DEV, which exists for exactly this and holds no live traffic.
//     Their contract is _meta/INTEGRATION_API_SPEC.md, which is the document they code against
//     anyway — Swagger was never the agreement.
//   · "Which build is this?" was the other thing the Swagger page was used for in production,
//     and it never actually answered it. GET /api/version does, and is anonymous for that reason.
//
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            $"/swagger/{ApiContract.Document}/swagger.json",
            $"Raphael Backend API {ApiContract.Document}");
    });
}

app.UseAuthentication(); // Who is the user?

app.UseDefaultFiles(); // So that it searches index.html if you access the root
app.UseStaticFiles();  // To serve files from wwwroot

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);
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
app.MapHub<DispatchHub>("/hubs/dispatch");

//
// Liveness. Anonymous because the thing that calls it is the App Service health check, which
// has no credential and never will; and because a probe that answers 401 is a probe that says
// "unhealthy" about a perfectly healthy instance, so the platform would restart it.
//
// Predicate false on purpose: it runs no checks. Answering at all is the whole signal — this
// process is up, accepting sockets and routing requests. It is not an opinion about the
// database, and it must not become one: see DatabaseHealthCheck.
//
// It carries nothing about the system in its body, which is why it can be public.
//
app.MapHealthChecks(
       HealthProbeTelemetryProcessor.LivenessPath,
       new HealthCheckOptions { Predicate = _ => false })
   .AllowAnonymous();

// Readiness, for a person. Behind authentication because "the database is unreachable" is a
// fact about the inside of the system and there is no reason to publish it.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(DatabaseHealthCheck.ReadyTag)
}).RequireAuthorization();

app.MapControllers();

//
// Bring the database up to this build, and stop if that cannot be done.
//
// ⚠️ This used to log the failure and carry on, and that is not a small difference. On
// 2026-09-18 the first Azure database applied its 51 migrations, failed to seed its users on
// an unmapped NOT NULL column, and the API started anyway — reporting healthy, serving
// Swagger, and unable to authenticate a single person. The exception was three hours down a
// container log that nobody had a reason to open.
//
// An API whose schema did not finish arriving is not a degraded API, it is one that will
// answer wrongly. Failing here costs a restart loop that says what is wrong on every pass;
// continuing costs an afternoon.
//
// Note this is deliberately louder than /health, which stays a liveness probe and says
// nothing about the database on purpose — see DatabaseHealthCheck.
//
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

    try
    {
        var initializer = services.GetRequiredService<IDbInitializer>();
        initializer.Initialize();
    }
    catch (Exception ex)
    {
        logger.LogCritical(
            ex,
            "Database initialisation failed. Refusing to start: this process would serve " +
            "requests against a database that is not in the shape this build expects.");
        throw;
    }

    // Firebase, built here on purpose rather than left to the first caller.
    //
    // It is a singleton, so it is otherwise constructed the first time somebody needs to
    // notify a driver -- which is to say, at the exact moment a missing credential costs a
    // notification and the furthest possible point from anyone who could act on it. Resolving
    // it now moves that answer into the startup log, next to the database's.
    //
    // It does NOT throw, unlike the block above, and the difference is deliberate: a database
    // in the wrong shape makes every answer suspect, while no Firebase credential costs push
    // notifications and nothing else. Refusing to start over the second would take dispatch
    // down to protect a feature.
    services.GetRequiredService<IFirebaseMessagingService>();
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

