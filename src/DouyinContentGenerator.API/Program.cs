using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using DouyinContentGenerator.API.Hubs;
using DouyinContentGenerator.API.Middleware;
using DouyinContentGenerator.API.Services;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Infrastructure.AI;
using DouyinContentGenerator.Infrastructure.Data;
using DouyinContentGenerator.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

// Hangfire
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.Queues = new[] { "default", "generation" };
});

// SignalR
builder.Services.AddSignalR();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new Exception("JWT Secret not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IGenerationTaskService, GenerationTaskService>();

// Register Budget Service (singleton - relies on Redis)
builder.Services.AddSingleton<IBudgetReservationService>(sp =>
{
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
    var config = sp.GetRequiredService<IConfiguration>();
    var dailyBudget = config.GetValue<decimal>("CostControl:DailyBudget", 200.0m);
    return new BudgetReservationService(redis, dailyBudget);
});

// Register AI Services
builder.Services.AddAIService(builder.Configuration);

// Register User-scoped AI Generator Factory (reads API keys from DB)
builder.Services.AddScoped<IUserAIGeneratorFactory, UserAIGeneratorFactory>();

var app = builder.Build();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Use EnsureCreated for dev to bypass Npgsql migration issue with Docker proxy
    await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db);
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Middleware pipeline
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<BudgetGuardMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SignalR Hub
app.MapHub<GenerationHub>("/hubs/generation");

// Hangfire Dashboard (development only)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard();
}

app.Run();
