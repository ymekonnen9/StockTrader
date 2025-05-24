// StockTrader.API/Program.cs
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

// --- DATABASE ---
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

// --- SERVICES ---
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<DataSeeder>();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("_myAllowSpecificOrigins", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// --- CONTROLLERS ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- SWAGGER ---
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen();
}

// --- AUTHENTICATION & IDENTITY (Uncomment when needed) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

Console.WriteLine($"ASPNETCORE_ENVIRONMENT is: {app.Environment.EnvironmentName}");

// --- MIDDLEWARE PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // app.UseHsts(); // Optional if not using HTTPS on production yet
}

// Fix for ALB HTTPS forwarding
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("_myAllowSpecificOrigins");

// app.UseAuthentication(); // Enable if using JWT or Identity
// app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

await SeedDatabaseAsync(app);

app.Run();

// --- DATABASE SEEDING ---
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
