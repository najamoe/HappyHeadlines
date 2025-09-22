using api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AfricaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AfricaConnection")));

builder.Services.AddDbContext<AsiaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AsiaConnection")));

builder.Services.AddDbContext<EuropeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EuropeConnection")));

builder.Services.AddDbContext<NorthAmericaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NorthAmericaConnection")));

builder.Services.AddDbContext<SouthAmericaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SouthAmericaConnection")));

builder.Services.AddDbContext<OceaniaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OceaniaConnection")));

builder.Services.AddDbContext<AntarcticaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AntarcticaConnection")));

builder.Services.AddDbContext<GlobalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GlobalConnection")));

var app = builder.Build();


app.UseSwagger();
app.UseStaticFiles();
app.UseSwaggerUI();


//app.UseHttpsRedirection();

app.MapControllers();

app.Run();


