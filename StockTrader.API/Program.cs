// In StockTrader.API/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockTrader.Application.Configuration; // Assuming this is your namespace for JwtSettings
using StockTrader.Application.Services;
using StockTrader.Domain.Entities;
using StockTrader.Infrastructure.Data;
using StockTrader.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Connection String & DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 42)), // Ensure this matches your RDS MySQL version reasonably well
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    ));

// 2. Configure ASP.NET Core Identity
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

// 3. Configure JWT Settings
var jwtSettings = new JwtSettings(); // Ensure JwtSettings class is defined correctly
builder.Configuration.Bind(JwtSettings.SectionName, jwtSettings);
builder.Services.AddSingleton(jwtSettings); // Makes JwtSettings available via DI

// 4. Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // More flexible: true if not Development
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

// 5. Add other services to the container.
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IOrderService, OrderService>();
// Assuming you might add other services like IPaymentService, IMarketDataService later

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // We'll configure Swagger for JWT later if needed

// builder.Services.AddAuthentication(); // This is redundant, already configured above
builder.Services.AddAuthorization(); // Registers authorization services

builder.Services.AddScoped<DataSeeder>(); // For seeding initial data

var app = builder.Build();

// 6. Seed Database (runs on application startup)
// This will attempt database operations. If the connection string is wrong or DB is unreachable,
// it will fail here and likely prevent the app from starting properly.
await SeedDatabaseAsync(app);

// 7. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage(); // Good for seeing detailed errors in dev
}
else
{
    // For production, you might want more robust error handling
    app.UseExceptionHandler("/Error"); // You'd need to create an Error handling mechanism/page
    app.UseHsts();
}

app.UseHttpsRedirection();

// ** CRITICAL ORDERING FOR MIDDLEWARE **
app.UseRouting(); // 1. Call UseRouting first to establish endpoint selection.

app.UseCors("_myAllowSpecificOrigins"); // 2. Call UseCors after UseRouting and before UseAuthentication/UseAuthorization
                                        //    (Assuming you have a CORS policy named "_myAllowSpecificOrigins" defined)
                                        //    If you don't have CORS yet, you can omit this line for now,
                                        //    but you'll need it for a separate frontend.

app.UseAuthentication(); // 3. Call UseAuthentication before UseAuthorization. THIS WAS MISSING!
app.UseAuthorization();  // 4. Now UseAuthorization.

app.MapControllers();    // 5. Map your controller endpoints.

app.Run();
// In StockTrader.API/Program.cs

async Task SeedDatabaseAsync(WebApplication webApp)
{
    using (var scope = webApp.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        // It's better to resolve DbContext inside the try block if its creation could fail,
        // but for CanConnectAsync, getting it here is fine.
        var context = services.GetRequiredService<ApplicationDbContext>();
        try
        {
            logger.LogInformation("<<<<< Attempting simple database connection test with CanConnectAsync... >>>>>");
            // You can try setting a longer command timeout specifically for this test if you suspect
            // an unusually slow initial handshake, though the default should be generous enough for a timeout.
            // context.Database.SetCommandTimeout(120); // e.g., 120 seconds

            var canConnect = await context.Database.CanConnectAsync();

            if (canConnect)
            {
                logger.LogInformation("<<<<< SUCCESS! Successfully connected to the database using CanConnectAsync! >>>>>");

                // For this test, we are NOT running migrations or full seeding.
                // If CanConnectAsync works, we can then re-introduce migrations and seeding.
                // logger.LogInformation("Applying database migrations if any...");
                // await context.Database.MigrateAsync();
                // var seeder = services.GetRequiredService<DataSeeder>();
                // logger.LogInformation("Attempting to seed initial data...");
                // await seeder.SeedAsync();
                // logger.LogInformation("Initial data seeding attempt completed.");
            }
            else
            {
                logger.LogError("<<<<< FAILURE! Failed to connect to the database using CanConnectAsync. Check RDS public accessibility, security groups, connection string details, and Fargate task outbound internet access. >>>>>");
            }
        }
        catch (Exception ex)
        {
            // This will catch the MySqlConnector.MySqlException if CanConnectAsync itself throws it due to timeout.
            logger.LogError(ex, "<<<<< EXCEPTION during CanConnectAsync database test. This likely means a connection timeout or other fundamental connection issue. >>>>>");
        }
    }
}