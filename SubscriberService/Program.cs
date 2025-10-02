using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriberService.Data;
using SubscriberService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<SubscriberDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SubscriberDatabase")));


builder.Services.AddSingleton<RabbitMQConnection>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();

var app = builder.Build();

//Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
