using Monitoring; // ✅ comes from shared class library
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add services like DbContext here...
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

// Log startup
MonitorService.Log.Information("DraftService is starting up...");

app.Run();
