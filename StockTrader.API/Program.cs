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
using TradeCraftExchange.Application.Configuration;
using TradeCraftExchange.Application.Services;
using TradeCraftExchange.Infrastructure.Services;
using TradeCraftExchange.Infrastructure.BackgroundServices;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)), // IMPORTANT: Update to your MySQL Server version
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
builder.Services.AddSingleton(jwtSettings);

var finnhubSettings = new FinnhubSettings();
builder.Configuration.Bind(FinnhubSettings.SectionName, finnhubSettings); 
builder.Services.AddSingleton(finnhubSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true; // Saves the token in the HttpContext
    options.RequireHttpsMetadata = builder.Environment.IsProduction(); // Only require HTTPS in production for easier local dev with HTTP if needed
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // Remove default 5-minute clock skew

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});
// Add services to the container.
builder.Services.AddScoped<ITokenService, JwtTokenService>();
//builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("FinnhubClient", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddScoped<IMarketDataService, FinnhubMarketDataService>();
//builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddHostedService<StockPriceUpdateService>();
var app = builder.Build();
await SeedDatabaseAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

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

            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync(); 

            logger.LogInformation("Attempting to seed initial data...");
            await seeder.SeedAsync();
            logger.LogInformation("Initial data seeding attempt completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database migration or seeding.");
 
        }
    }
}