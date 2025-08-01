
using System.Text;
using Raphael.Shared.DbContexts;
using Raphael.UsersService.Data;
using Raphael.UsersService.Services;
using Raphael.UsersService.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// SwaggerDoc
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Raphael Users Service API",
        Version = "v1"
    });
});
//builder.Services.AddSingleton<IUserService, UserService>();



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
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaphaelConnection")));
/*builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));*/

// Inject services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();    

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Users Service API v1");
});

// Swagger (optional)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

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


    // Initialize database
    /*using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<UsersDbContext>();
        Raphael.UsersService.Data.DbInitializer.Seed(context);
    }*/

    // usuario admin, sin Hash
    //DbInitializer.Seed2(app);

    // usando clase SeedData
    /*using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        try
        {
            SeedData.Initialize(services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seeding error: {ex.Message}");
        }
    }*/


    app.Run();




