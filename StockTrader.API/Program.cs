// In StockTrader.API/Program.cs
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
var jwtSettings = new JwtSettings();
builder.Configuration.Bind(JwtSettings.SectionName, jwtSettings);
builder.Services.AddSingleton(jwtSettings);

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
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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

// 5. Register Application Services
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<DataSeeder>();

// 6. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("_myAllowSpecificOrigins", policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:3000") // For local React dev
                       .AllowAnyHeader()
                       .AllowAnyMethod();
        // TODO: Add your deployed frontend origin here when you have one
    });
});

// 7. Add Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "StockTrader API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// 8. Add Authorization
builder.Services.AddAuthorization();

// 9. Build App
var app = builder.Build();

Console.WriteLine($"ASPNETCORE_ENVIRONMENT is: {app.Environment.EnvironmentName}"); // For debugging

// 10. Seed Database
await SeedDatabaseAsync(app);

// 11. Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger(); // Enable Swagger JSON generation
    app.UseSwaggerUI(options => // Enable Swagger UI
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "StockTrader API V1");
        // By default, RoutePrefix is "swagger", so UI will be at /swagger
        // To serve at root: options.RoutePrefix = string.Empty;
    });
}
else
{
    app.UseExceptionHandler("/Error"); // You would create a proper error handling page/mechanism
    app.UseHsts();
}

// HTTPS Redirection: Usually handled by ALB if it terminates SSL.
// If ALB forwards HTTP to Fargate, app.UseHttpsRedirection() in the container
// might cause redirect loops if not configured carefully with forwarded headers.
// For simplicity, if ALB handles HTTPS, you might not need this here.
// app.UseHttpsRedirection();

app.UseStaticFiles(); // If you have any static files in wwwroot
app.UseRouting();
app.UseCors("_myAllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
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
        logger.LogInformation("<<<<< Checking DB connection with CanConnectAsync... >>>>>");
        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("<<<<< SUCCESS: Connected to DB! >>>>>");
            logger.LogInformation("Applying migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("<<<<< Migrations applied successfully. >>>>>");

            // Uncomment if seeding is needed and DataSeeder is implemented
            // var seeder = services.GetRequiredService<DataSeeder>();
            // logger.LogInformation("Seeding initial data...");
            // await seeder.SeedAsync();
            // logger.LogInformation("Data seeded.");
        }
        else
        {
            logger.LogError("<<<<< FAILED: Cannot connect to DB. Check RDS/public access/connection string. >>>>>");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "<<<<< EXCEPTION during DB initialization. >>>>>");
    }
}
