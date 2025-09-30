using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Instrumentation.Http;
using PublisherService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation() // Monitor incoming HTTP requests
            .AddHttpClientInstrumentation() // Monitor outgoing HTTP requests
            .AddZipkinExporter(); // Export traces to view in Zipkin
    });



// Services

builder.Services.AddSingleton<RabbitMqPublisher>(); 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PublisherService API V1");
});
app.MapControllers();

app.Run();
