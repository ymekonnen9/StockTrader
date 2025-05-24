// In StockTrader.API/Program.cs
// using Microsoft.AspNetCore.Authentication.JwtBearer; // Keep if AddAuthentication is kept for AddControllers
// using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// using Microsoft.IdentityModel.Tokens;
// using StockTrader.Application.Configuration;
// using StockTrader.Application.Services;
// using StockTrader.Domain.Entities;
using StockTrader.Infrastructure.Data;
// using StockTrader.Infrastructure.Services;
// using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Minimal services needed for controllers and DB context (if controllers use it, TestController doesn't yet)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 42)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    ));





builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer(); // Not needed for this direct controller test

var app = builder.Build();

Console.WriteLine($"ASPNETCORE_ENVIRONMENT is: {app.Environment.EnvironmentName}");
// await SeedDatabaseAsync(app); // Keep seeding commented out for this test

if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

app.UseRouting();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers(); // This maps your controller routes

app.Run();





//builder.Services.AddControllers(); // Registers controller services
// builder.Services.AddEndpointsApiExplorer(); // Not strictly needed if not using Swagger for this test
// builder.Services.AddSwaggerGen(); // Disable Swagger for this minimal test

// builder.Services.AddIdentity<ApplicationUser, IdentityRole>() ... // Disable Identity for this minimal test
// builder.Services.AddAuthentication(...) ... // Disable AuthN/AuthZ for this minimal test
// builder.Services.AddAuthorization();

// builder.Services.AddScoped<ITokenService, JwtTokenService>(); // Disable other services for this test
// builder.Services.AddScoped<IStockService, StockService>();
// builder.Services.AddScoped<IPortfolioService, PortfolioService>();
// builder.Services.AddScoped<IOrderService, OrderService>();
// builder.Services.AddScoped<DataSeeder>();
// builder.Services.AddCors(...);


//var app = builder.Build();

//Console.WriteLine($"ASPNETCORE_ENVIRONMENT is: {app.Environment.EnvironmentName}"); // For debugging

// Temporarily comment out seeding to simplify startup further for this test
// await SeedDatabaseAsync(app);

// --- MINIMAL MIDDLEWARE PIPELINE ---
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//}
//else
//{
//    app.UseExceptionHandler("/Error");
//    // app.UseHsts(); // Optional for this test
//}

// app.UseHttpsRedirection(); // Can be problematic behind ALB if not configured for forwarded headers

//app.UseRouting();

// app.UseCors("_myAllowSpecificOrigins"); // Disable CORS for this minimal test
// app.UseAuthentication(); // Disable for this minimal test
// app.UseAuthorization(); // Disable for this minimal test

//app.MapGet("/health", () => Results.Ok(new { status = "healthy" })); // Keep your health check
//app.MapControllers(); // This maps your controller routes

//app.Run();


// Keep SeedDatabaseAsync method definition, but it won't be called if line above is commented out.
async Task SeedDatabaseAsync(WebApplication webApp)
{
    // ... (your existing seed method)
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();
    try
    {
        logger.LogInformation("<<<<< Checking DB connection with CanConnectAsync... >>>>>");
        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("<<<<< SUCCESS: Connected to DB! >>>>>");
            logger.LogInformation("Applying migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("<<<<< Migrations applied successfully. >>>>>");
        }
        else
        {
            logger.LogError("<<<<< FAILED: Cannot connect to DB. >>>>>");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "<<<<< EXCEPTION during DB initialization. >>>>>");
    }
}