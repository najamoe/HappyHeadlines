using DraftService;
using DraftService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DraftDatabase");

// Register EF Core DbContext
builder.Services.AddDbContext<DraftDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
