using CommentService.Data;
using CacheService.Services;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Prometheus;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

//Setup for server docker/local

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var server = isDocker ? "host.docker.internal,1433" : "localhost,1433";

var connectionString = $"Server={server};Database=CommentDB;User Id=sa;Password=StrongPassw0rd!@#2025;Encrypt=True;TrustServerCertificate=True;";

// --- Database ---
builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Monitoring initialization ---
_ = MonitorService.Log;

// --- Services / Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// --- Redis and Comment Cache setup ---
var isDesignTime = AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Design", StringComparison.OrdinalIgnoreCase) == true);

if (!isDesignTime)
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379,abortConnect=false";
    Console.WriteLine($"Using Redis connection string: {redisConnectionString}");

    builder.Services.AddSingleton(new RedisCacheService(redisConnectionString));
    builder.Services.AddSingleton<CommentCacheService>();
}
else
{
    Console.WriteLine("Skipping Redis setup during EF migrations...");
}

// --- HTTP Clients for other services ---
builder.Services.AddHttpClient("ArticleService", c =>
{
    c.BaseAddress = new Uri("http://articleservice:8080");
});

builder.Services.AddHttpClient("ProfanityService", c =>
{
    c.BaseAddress = new Uri("http://profanityservice:8080");
});

// --- Prometheus metrics ---
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder => builder.AddPrometheusExporter());

var app = builder.Build();

// --- Middleware ---
app.UseHttpMetrics();
app.MapMetrics();

// app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// --- Swagger ---
app.UseSwagger();
app.UseSwaggerUI();


app.Run();
