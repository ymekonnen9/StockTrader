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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 42)), // Ensure this version matches your MySQL server
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    ));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;

}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

var jwtSettings = new JwtSettings();
builder.Configuration.Bind(JwtSettings.SectionName, jwtSettings);
builder.Services.AddSingleton(jwtSettings); // Make JwtSettings available for injection

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
    options.TokenValidationParameters = new TokenValidationParameters()
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

// Add services to the container.
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization(); // Ensure Authorization services are added
builder.Services.AddScoped<DataSeeder>(); // Still needed if you re-enable seeding
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Temporarily disable database seeding and migration for troubleshooting
// await SeedDatabaseAsync(app); 

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// It's generally good practice to have UseRouting() before UseAuthentication() and UseAuthorization()
// However, in .NET 6+ minimal APIs, this is often handled implicitly.
// If you encounter issues, explicitly adding app.UseRouting(); might be necessary.

app.UseHttpsRedirection(); // Important for production

// Ensure Authentication middleware is added before Authorization
app.UseAuthentication(); // This was missing and is crucial for [Authorize] to work
app.UseAuthorization();

app.MapControllers();

app.Run();

// SeedDatabaseAsync is kept for when you want to re-enable it
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
            // Temporarily disable migration for troubleshooting
            // await context.Database.MigrateAsync(); 

            logger.LogInformation("Attempting to seed initial data (currently disabled for troubleshooting)...");
            // Temporarily disable seeding for troubleshooting
            // await seeder.SeedAsync(); 
            logger.LogInformation("Initial data seeding attempt completed (currently disabled).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database migration or seeding (currently disabled).");
        }
    }
}
