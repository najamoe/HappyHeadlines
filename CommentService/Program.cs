using Microsoft.EntityFrameworkCore;
using Polly;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using CommentService.Data;



var builder = WebApplication.CreateBuilder(args);
var circuitBreakerPolicy = GetCircuitBreakerPolicy();

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) // Error 503 (Service unavailable)
                 .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(30));
}
// If 3 failures occur, the breaker "opens" for 30 seconds. During this time, all calls will fail immediately.
// After 30 seconds, the next call will be allowed to pass through. If it succeeds, the circuit "closes" and normal operation resumes.


// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CommentDatabase")));

// Register named HttpClient for ProfanityService
builder.Services.AddHttpClient("ProfanityService", c =>
{
    c.BaseAddress = new Uri("http://profanityservice:80/");
})
.AddPolicyHandler(circuitBreakerPolicy);
builder.Services.AddHttpClient("ArticleService", client =>
{
    client.BaseAddress = new Uri("https://articleservice:80/"); // actual URL of ArticleService
});


var app = builder.Build();


// swagger setup
app.UseSwagger();
app.UseSwaggerUI();





app.MapControllers();

app.Run();
