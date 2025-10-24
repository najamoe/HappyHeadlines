using ArticleService.Infrastructure;
using ArticleService.Models;
using CacheService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Models;
using Shared.Profanity;
using System.Diagnostics;

namespace ArticleService.Controllers
{
    [Route("article")]
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private readonly Dictionary<string, DbContext> _dbContexts;
        private readonly GlobalDbContext _global;
        private readonly ArticleCacheService _cache;
        private readonly ProfanityClient _profanity;

        public ArticleController(
            AfricaDbContext africa,
            AsiaDbContext asia,
            EuropeDbContext europe,
            NorthAmericaDbContext northAmerica,
            SouthAmericaDbContext southAmerica,
            OceaniaDbContext oceania,
            AntarcticaDbContext antarctica,
            GlobalDbContext global,
            ArticleCacheService cache,
            ProfanityClient profanity)
        {
            _dbContexts = new(StringComparer.OrdinalIgnoreCase)
            {
                ["africa"] = africa,
                ["asia"] = asia,
                ["europe"] = europe,
                ["northamerica"] = northAmerica,
                ["southamerica"] = southAmerica,
                ["oceania"] = oceania,
                ["antarctica"] = antarctica
            };
            _global = global;
            _cache = cache;
            _profanity = profanity;
        }

        private DbContext GetDb(string continent)
        {
            if (string.IsNullOrWhiteSpace(continent))
                throw new ArgumentException("Continent required");
            if (!_dbContexts.TryGetValue(continent, out var db))
                throw new ArgumentException($"Unknown continent: {continent}");
            return db;
        }

        [HttpGet("by-continent/{continent}")]
        public async Task<IActionResult> GetAllByContinent(string continent)
        {
            using var activity = MonitorService.ActivitySource.StartActivity("GetAllByContinent");
            try
            {
                var db = GetDb(continent);
                var articles = await db.Set<Article>().ToListAsync();
                MonitorService.Log.Information("Fetched {Count} articles for {Continent}", articles.Count, continent);
                return Ok(articles);
            }
            catch (Exception ex)
            {
                MonitorService.Log.Error(ex, "Error fetching articles for {Continent}", continent);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromQuery] string continent, [FromRoute] int id)
        {
            using var activity = MonitorService.ActivitySource.StartActivity("GetArticleById");

            try
            {
                if (continent.Equals("global", StringComparison.OrdinalIgnoreCase))
                {
                    // Try cache for GlobalDB
                    var cached = await _cache.GetArticleAsync(id);
                    if (cached != null)
                    {
                        MonitorService.Log.Information("Cache hit for article {Id}", id);
                        return Ok(cached);
                    }

                    MonitorService.Log.Information("Cache miss for article {Id}", id);

                    var globalArticle = await _global.Set<GlobalArticle>().FindAsync(id);
                    if (globalArticle == null)
                        return NotFound();

                    var globalDto = new ArticleDto
                    {
                        Id = globalArticle.Id,
                        Title = globalArticle.Title,
                        Content = globalArticle.Content,
                        Author = globalArticle.Author,
                        PublishedAt = globalArticle.PublishedAt,
                        SourceArticleId = globalArticle.SourceArticleId,
                        Continent = globalArticle.SourceContinent ?? "Unknown",
                        TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                    };

                    await _cache.SetArticleAsync(globalDto);
                    return Ok(globalDto);
                }

                // For continent DBs
                var db = GetDb(continent);
                var article = await db.Set<Article>().FindAsync(id);
                if (article == null)
                    return NotFound();

                var dto = new ArticleDto
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    Author = article.Author,
                    PublishedAt = article.PublishedAt,
                    Continent = article.Continent,
                    TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                MonitorService.Log.Error(ex, "Error fetching article {Id}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string continent, [FromBody] Article article)
        {
            if (article == null)
                return BadRequest("Article is null.");

            using var activity = MonitorService.ActivitySource.StartActivity("CreateArticle");

            try
            {
                var text = $"{article.Title} {article.Content}";
                if (!await _profanity.IsCleanAsync(text))
                    return BadRequest("Article contains profanity.");

                // Save to continent DB
                var db = GetDb(continent);
                article.Continent = continent;
                db.Set<Article>().Add(article);
                await db.SaveChangesAsync();

                // Create global copy
                var globalArticle = new GlobalArticle
                {
                    Author = article.Author,
                    Title = article.Title,
                    Content = article.Content,
                    PublishedAt = article.PublishedAt,
                    SourceArticleId = article.Id,
                    SourceContinent = continent
                };

                _global.Set<GlobalArticle>().Add(globalArticle);
                await _global.SaveChangesAsync();

                // Cache global version
                var dto = new ArticleDto
                {
                    Id = globalArticle.Id,
                    Title = globalArticle.Title,
                    Content = globalArticle.Content,
                    Author = globalArticle.Author,
                    PublishedAt = globalArticle.PublishedAt,
                    SourceArticleId = globalArticle.SourceArticleId,
                    Continent = globalArticle.SourceContinent ?? "Global",
                    TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                };

                await _cache.SetArticleAsync(dto);

                MonitorService.Log.Information(
                    "Created article {ContinentId} in {Continent}, global copy {GlobalId}",
                    article.Id, continent, globalArticle.Id);

                return CreatedAtAction(nameof(GetById),
                    new { id = globalArticle.Id, continent = "global" },
                    new { ContinentId = article.Id, GlobalId = globalArticle.Id });
            }
            catch (Exception ex)
            {
                MonitorService.Log.Error(ex, "Error creating article in {Continent}", continent);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromQuery] string continent, [FromRoute] int id, [FromBody] Article update)
        {
            if (update == null || update.Id != id)
                return BadRequest();

            var db = GetDb(continent);
            var existing = await db.Set<Article>().FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Title = update.Title;
            existing.Content = update.Content;
            existing.Author = update.Author;
            await db.SaveChangesAsync();

            // Update corresponding global copy
            var globalCopy = await _global.Set<GlobalArticle>()
                .FirstOrDefaultAsync(a => a.SourceArticleId == id && a.SourceContinent == continent);

            if (globalCopy != null)
            {
                globalCopy.Title = existing.Title;
                globalCopy.Content = existing.Content;
                globalCopy.Author = existing.Author;
                await _global.SaveChangesAsync();

                var dto = new ArticleDto
                {
                    Id = globalCopy.Id,
                    Title = globalCopy.Title,
                    Content = globalCopy.Content,
                    Author = globalCopy.Author,
                    PublishedAt = globalCopy.PublishedAt,
                    SourceArticleId = globalCopy.SourceArticleId,
                    Continent = globalCopy.SourceContinent ?? "Global",
                    TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                };
                await _cache.SetArticleAsync(dto);
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromQuery] string continent, [FromRoute] int id)
        {
            var db = GetDb(continent);
            var article = await db.Set<Article>().FindAsync(id);
            if (article == null)
                return NotFound();

            db.Remove(article);
            await db.SaveChangesAsync();

            // Delete global copy + cache
            var globalCopy = await _global.Set<GlobalArticle>()
                .FirstOrDefaultAsync(a => a.SourceArticleId == id && a.SourceContinent == continent);
            if (globalCopy != null)
            {
                _global.Remove(globalCopy);
                await _global.SaveChangesAsync();
                await _cache.RemoveArticleAsync(globalCopy.Id);
            }

            return NoContent();
        }
    }
}
