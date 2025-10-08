using ArticleService.Infrastructure;
using CacheService.Services;
using ArticleService.Consumers;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Prometheus;
using OpenTelemetry.Metrics;

// --- Monitoring / Logging ---
_ = MonitorService.Log;

var builder = WebApplication.CreateBuilder(args);

// --- MVC / Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Dynamic SQL Server connection setup ---
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var server = isDocker ? "host.docker.internal,1433" : "localhost,1433";

// Helper function for clean setup
string BuildConnection(string dbName) =>
    $"Server={server};Database={dbName};User Id=sa;Password=StrongPassw0rd!@#2025;Encrypt=True;TrustServerCertificate=True;";

// --- Databases ---
builder.Services.AddDbContext<AfricaDbContext>(o => o.UseSqlServer(BuildConnection("AfricaDB")));
builder.Services.AddDbContext<AsiaDbContext>(o => o.UseSqlServer(BuildConnection("AsiaDB")));
builder.Services.AddDbContext<EuropeDbContext>(o => o.UseSqlServer(BuildConnection("EuropeDB")));
builder.Services.AddDbContext<NorthAmericaDbContext>(o => o.UseSqlServer(BuildConnection("NorthAmericaDB")));
builder.Services.AddDbContext<SouthAmericaDbContext>(o => o.UseSqlServer(BuildConnection("SouthAmericaDB")));
builder.Services.AddDbContext<OceaniaDbContext>(o => o.UseSqlServer(BuildConnection("OceaniaDB")));
builder.Services.AddDbContext<AntarcticaDbContext>(o => o.UseSqlServer(BuildConnection("AntarcticaDB")));
builder.Services.AddDbContext<GlobalDbContext>(o => o.UseSqlServer(BuildConnection("GlobalDB")));

// --- RabbitMQ ---
builder.Services.AddHostedService<ArticleConsumer>();

// --- ProfanityService ---
builder.Services.AddHttpClient("ProfanityService", c =>
{
    c.BaseAddress = new Uri("http://profanityservice:8080");
});

// --- Redis + Article Cache ---
var isDesignTime = AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Design", StringComparison.OrdinalIgnoreCase) == true);

if (!isDesignTime)
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379,abortConnect=false";
    Console.WriteLine($"Using Redis connection string: {redisConnectionString}");

    builder.Services.AddSingleton<RedisCacheService>(_ => new RedisCacheService(redisConnectionString));
    builder.Services.AddSingleton<ArticleCacheService>();
    builder.Services.AddHostedService<ArticleCacheBackgroundService>();
}
else
{
    Console.WriteLine("Skipping Redis and Background Services during EF migrations...");
}

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// --- Build app ---
var app = builder.Build();

app.UseHttpMetrics();
app.MapMetrics();

app.UseCors();
app.UseSwagger();
app.UseStaticFiles();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
