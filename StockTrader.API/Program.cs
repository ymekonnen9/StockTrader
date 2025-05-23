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
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // true if not Development
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

// 5. Register Services
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<DataSeeder>(); // For seeding initial data

// 6. Add CORS Policy (ensure "_myAllowSpecificOrigins" is defined or adjust as needed)
// For now, assuming you have a React frontend running on http://localhost:3000 for local dev
// For AWS deployment, your ALB/custom domain would be a different origin.
builder.Services.AddCors(options =>
{
    options.AddPolicy("_myAllowSpecificOrigins", policyBuilder => // Renamed builder to policyBuilder for clarity
    {
        // For development with local React app
        policyBuilder.WithOrigins("http://localhost:3000") // If your React app runs on port 3000
               .AllowAnyHeader()
               .AllowAnyMethod();

    });
});

// 7. Add Swagger and API controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => // Added options for JWT in Swagger
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "StockTrader API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter into field the word 'Bearer' followed by a space and the JWT value",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
    {
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }});
});


// 8. Add Authorization
builder.Services.AddAuthorization();

// 9. Build App
var app = builder.Build();

// 10. Seed Database (runs on application startup)
await SeedDatabaseAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseRouting();
app.UseCors("_myAllowSpecificOrigins"); // Ensure this policy is correctly defined
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })); // Your health check
app.MapControllers();

app.Run();


// Helper method for seeding the database
async Task SeedDatabaseAsync(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();
    try
    {
        logger.LogInformation("<<<<< Attempting simple database connection test with CanConnectAsync... >>>>>");
        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("<<<<< SUCCESS! Successfully connected to the database using CanConnectAsync! >>>>>");

            logger.LogInformation("Applying database migrations if any...");
            await context.Database.MigrateAsync(); // This will create tables if they don't exist
            logger.LogInformation("<<<<< SUCCESS! Database migrations applied (or database was up-to-date). >>>>>");

            // Keep full seeding commented out for this specific test phase
            // var seeder = services.GetRequiredService<DataSeeder>();
            // logger.LogInformation("Attempting to seed initial data...");
            // await seeder.SeedAsync();
            // logger.LogInformation("Initial data seeding attempt completed.");
        }
        else
        {
            logger.LogError("<<<<< FAILURE! Cannot connect to the database using CanConnectAsync. Check RDS public accessibility, security groups, connection string details, and Fargate task outbound internet access. >>>>>");
        }
    }
    catch (Exception ex)
    {
        // This will catch exceptions from CanConnectAsync OR MigrateAsync
        logger.LogError(ex, "<<<<< EXCEPTION during database initialization (CanConnectAsync or MigrateAsync). >>>>>");
    }
}
