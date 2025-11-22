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
                ["antarctica"] = antarctica,
                ["global"] = global
            };

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

                // Cache article
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
                await _cache.SetArticleAsync(dto);

                MonitorService.Log.Information(
                    "Created article {ContinentId} in {Continent}",
                    article.Id, continent);

                return CreatedAtAction(nameof(GetById),
                    new { id = article.Id, continent = continent },
                    dto);
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

            // Cache updated article
            var dto = new ArticleDto
            {
                Id = existing.Id,
                Title = existing.Title,
                Content = existing.Content,
                Author = existing.Author,
                PublishedAt = existing.PublishedAt,
                Continent = existing.Continent,
                TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
            };
            await _cache.SetArticleAsync(dto);

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

            await _cache.RemoveArticleAsync(article.Id);

            return NoContent();
        }
    }
    }
