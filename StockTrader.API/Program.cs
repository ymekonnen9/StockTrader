using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockTrader.Application.Configuration;
using StockTrader.Application.Services;
using StockTrader.Domain.Entities;
using StockTrader.Infrastructure.Data;
using StockTrader.Infrastructure.Services;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

// Configure DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 42)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    )
);

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = new JwtSettings();
builder.Configuration.Bind(JwtSettings.SectionName, jwtSettings);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

// Register Services
builder.Services.AddAuthorization();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Optional: Seed the database
await SeedDatabaseAsync(app);

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Diagnostic Endpoint
app.MapGet("/di-test", (IServiceProvider services, ILogger<Program> logger) =>
{
    logger.LogInformation("--- Starting DI Test ---");
    try
    {
        var dbContext = services.GetService<ApplicationDbContext>();
        if (dbContext == null)
        {
            logger.LogError("DI TEST FAILED: ApplicationDbContext is null.");
            return Results.Problem("Failed to resolve ApplicationDbContext.");
        }
        logger.LogInformation("DI TEST SUCCESS: ApplicationDbContext resolved successfully.");

        var userManager = services.GetService<UserManager<ApplicationUser>>();
        if (userManager == null)
        {
            logger.LogError("DI TEST FAILED: UserManager<ApplicationUser> is null.");
            return Results.Problem("Failed to resolve UserManager.");
        }
        logger.LogInformation("DI TEST SUCCESS: UserManager resolved successfully.");

        logger.LogInformation("--- DI Test Completed Successfully ---");
        return Results.Ok("All tested services were resolved successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DI TEST FAILED: An exception was thrown during service resolution.");
        return Results.Problem($"An exception occurred: {ex.Message}");
    }
});

app.Run();
async Task SeedDatabaseAsync(WebApplication webApp)
{
    using (var scope = webApp.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var seeder = services.GetRequiredService<DataSeeder>();

            logger.LogInformation("Applying database migrations (currently disabled for troubleshooting)...");
            //Temporarily disable migration for troubleshooting
            await context.Database.MigrateAsync();

           logger.LogInformation("Attempting to seed initial data (currently disabled for troubleshooting)...");
            //Temporarily disable seeding for troubleshooting
            await seeder.SeedAsync();
           logger.LogInformation("Initial data seeding attempt completed (currently disabled).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database migration or seeding (currently disabled).");
        }
    }
}
