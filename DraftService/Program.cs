using DraftService;
using DraftService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DraftDatabase");

// --- Dynamic SQL Server connection setup ---
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var server = isDocker ? "host.docker.internal,1433" : "localhost,1433";

string BuildConnection(string dbName) =>
    $"Server={server};Database={dbName};User Id=sa;Password=StrongPassw0rd!@#2025;Encrypt=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<DraftDbContext>(options =>
    options.UseSqlServer(
        BuildConnection("DraftDB"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
