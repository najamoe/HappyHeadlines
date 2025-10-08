using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArticleService.Infrastructure;
using ArticleService.Models;
using CacheService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using Polly.CircuitBreaker;
using RabbitMQ.Client;


namespace ArticleService.Controllers
{
    [Route("article")]
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private readonly AfricaDbContext _africa;
        private readonly AsiaDbContext _asia;
        private readonly EuropeDbContext _europe;
        private readonly NorthAmericaDbContext _northAmerica;
        private readonly SouthAmericaDbContext _southAmerica;
        private readonly OceaniaDbContext _oceania;
        private readonly AntarcticaDbContext _antarctica;
        private readonly GlobalDbContext _global;
        private readonly ArticleCacheService _cacheService;

        public ArticleController(
            AfricaDbContext africa,
            AsiaDbContext asia,
            EuropeDbContext europe,
            NorthAmericaDbContext northAmerica,
            SouthAmericaDbContext southAmerica,
            OceaniaDbContext oceania,
            AntarcticaDbContext antarctica,
            GlobalDbContext global,
            ArticleCacheService articleCacheService)
        {
            _africa = africa;
            _asia = asia;
            _europe = europe;
            _northAmerica = northAmerica;
            _southAmerica = southAmerica;
            _oceania = oceania;
            _antarctica = antarctica;
            _global = global;
            _cacheService = articleCacheService;
        }

        // Helper to select DbContext based on continent
        private DbContext GetDbContext(string continent)
        {
            return continent.ToLower() switch
            {
                "africa" => _africa,
                "asia" => _asia,
                "europe" => _europe,
                "northamerica" => _northAmerica,
                "southamerica" => _southAmerica,
                "oceania" => _oceania,
                "antarctica" => _antarctica,
                "global" => _global,
                _ => throw new Exception("Unknown continent")
            };
        }

        [HttpGet("by-continent/{continent}")]
        public async Task<IActionResult> GetAllByContinent([FromRoute] string continent)
        {
            using var activity = MonitorService.ActivitySource.StartActivity("GetAllByContinent");
            try
            {
                var db = GetDbContext(continent);
                var articles = await db.Set<Article>().ToListAsync();

                await Task.Delay(3000); // Simulate processing delay

                MonitorService.Log.Information("Fetched {Count} articles for continent {Continent}", articles.Count, continent);
                return Ok(articles);
            }
            catch (Exception ex)
            {
                MonitorService.Log.Error(ex, "Error fetching articles for continent {Continent}", continent);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromQuery] string continent, [FromRoute] int id)
        {
            using var activity = Monitoring.MonitorService.ActivitySource.StartActivity("GetArticleById");

            try
            {
                // Try cache first
                var cachedArticle = await _cacheService.GetArticleAsync(continent.ToLower(), id);
                if (cachedArticle != null)
                {
                    Monitoring.MonitorService.Log.Information("Cache hit for article {ArticleId} ({Continent})", id, continent);
                    return Ok(cachedArticle);
                }

                Monitoring.MonitorService.Log.Information("Cache miss for article {ArticleId} ({Continent})", id, continent);

                // Fetch from DB
                var db = GetDbContext(continent);
                var article = await db.Set<Article>().FindAsync(id);
                if (article == null)
                    return NotFound();

                var dto = new CacheService.Dtos.ArticleDto
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    Author = article.Author,
                    PublishedAt = article.PublishedAt
                };

                await _cacheService.SetArticleAsync(continent.ToLower(), dto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                Monitoring.MonitorService.Log.Error(ex, "Error fetching article {ArticleId} from {Continent}", id, continent);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string continent, [FromBody] Article article, [FromServices] IHttpClientFactory httpClientFactory)
        {
            if (article == null)
                return BadRequest("Article is null.");

            if (string.IsNullOrWhiteSpace(continent))
                return BadRequest("Continent is required.");

            using var activity = MonitorService.ActivitySource.StartActivity("CreateArticle");

            var _profanityClient = httpClientFactory.CreateClient("ProfanityService");

            try
            {
                // --- Profanity check on Title and Content ---
                var textToCheck = $"{article.Title} {article.Content}";
                var json = JsonSerializer.Serialize(new { text = textToCheck });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                MonitorService.Log.Information("Sending article text to ProfanityService: {Json}", json);

                var response = await _profanityClient.PostAsync("/api/profanity/check", content);
                using var profanityCheck = MonitorService.ActivitySource.StartActivity("CallProfanityService");
                MonitorService.Log.Information("ProfanityService responded with status code: {StatusCode}", response.StatusCode);

                var result = await response.Content.ReadAsStringAsync();
                MonitorService.Log.Information("ProfanityService response body: {ResponseBody}", result);

                if (!response.IsSuccessStatusCode)
                {
                    MonitorService.Log.Error("Error checking profanity: {StatusCode}", response.StatusCode);
                    return StatusCode((int)response.StatusCode, "Error checking profanity.");
                }

                var check = JsonSerializer.Deserialize<ProfanityCheckResult>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (check != null && !check.isClean)
                {
                    MonitorService.Log.Warning("Article contains profanity in title or content.");
                    return BadRequest("Article contains profanity and cannot be published.");
                }

                // --- Save Article if clean ---
                var db = GetDbContext(continent);
                db.Set<Article>().Add(article);
                
                await db.SaveChangesAsync();
                using var dbSave = MonitorService.ActivitySource.StartActivity("SaveArticleToDatabase");

                using (var cacheActivity = MonitorService.ActivitySource.StartActivity("AddArticleToCache"))
                {
                    var dto = new CacheService.Dtos.ArticleDto
                    {
                        Id = article.Id,
                        Title = article.Title,
                        Content = article.Content,
                        Author = article.Author,
                        PublishedAt = article.PublishedAt
                    };

                    await _cacheService.SetArticleAsync(continent.ToLower(), dto);
                }

                // --- Publish to RabbitMQ (simple trace) ---
                using var publishActivity = MonitorService.ActivitySource.StartActivity("PublishArticleEvent");

                var factory = new ConnectionFactory
                {
                    HostName = "rabbitmq",
                    UserName = "guest",
                    Password = "guest",
                    VirtualHost = "/",
                    Port = 5672
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: "ArticleQueue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

               

                var articleDto = new
                {
                    Id = article.Id,
                    article.Title,
                    article.Content,
                    article.Author,
                    article.PublishedAt,
                    TraceId = Activity.Current?.TraceId.ToString() // simple trace linkage
                };

                var message = JsonSerializer.Serialize(articleDto);
                var body = Encoding.UTF8.GetBytes(message);

                var props = new BasicProperties();
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "ArticleQueue",
                    mandatory: true,
                    basicProperties: props,
                    body: body
                );

                MonitorService.Log.Information("Published article {Title} to queue", article.Title);


                MonitorService.Log.Information("Article {ArticleId} created successfully in {Continent}", article.Id, continent);
                return CreatedAtAction(nameof(GetById), new { id = article.Id, continent }, article);
            }
            catch (BrokenCircuitException ex)
            {
                MonitorService.Log.Error(ex, "Profanity service is unavailable (circuit breaker).");
                return StatusCode(503, "Profanity service is unavailable. Please try again later.");
            }
            catch (HttpRequestException ex)
            {
                MonitorService.Log.Error(ex, "Error connecting to ProfanityService.");
                return StatusCode(503, "Error connecting to ProfanityService.");
            }
            catch (JsonException ex)
            {
                MonitorService.Log.Error(ex, "Failed to deserialize ProfanityService response.");
                return StatusCode(500, "Invalid response from ProfanityService.");
            }
        }

        public class ProfanityCheckResult
        {
            public bool isClean { get; set; }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromQuery] string continent, [FromRoute] int id, [FromBody] Article updatedArticle)
        {
            if (updatedArticle == null || updatedArticle.Id != id) return BadRequest();

            var db = GetDbContext(continent);
            var existingArticle = await db.Set<Article>().FindAsync(id);
            if (existingArticle == null) return NotFound();

            existingArticle.Author = updatedArticle.Author;
            existingArticle.Title = updatedArticle.Title;
            existingArticle.Content = updatedArticle.Content;

            await db.SaveChangesAsync();

            var dto = new CacheService.Dtos.ArticleDto
            {
                Id = existingArticle.Id,
                Title = existingArticle.Title,
                Content = existingArticle.Content,
                Author = existingArticle.Author,
                PublishedAt = existingArticle.PublishedAt

            };

            await _cacheService.SetArticleAsync(continent.ToLower(), dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromQuery] string continent, [FromRoute] int id)
        {
            var db = GetDbContext(continent);
            var article = await db.Set<Article>().FindAsync(id);
            if (article == null) return NotFound();

            db.Set<Article>().Remove(article);
            await db.SaveChangesAsync();

            await _cacheService.RemoveArticleAsync(continent.ToLower(), article.Id);
            return NoContent();
        }
    }
}
