using ProfanityService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<ProfanityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProfanityDatabase")));

var app = builder.Build();
app.MapControllers();

app.Run();
