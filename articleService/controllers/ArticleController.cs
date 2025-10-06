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
                var cachedArticle = await _cacheService.GetArticleAsync(continent.ToLower(), id.ToString());
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
                    Id = article.Id.ToString(),
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
        public async Task<IActionResult> Create([FromQuery] string continent, [FromBody] Article article)
        {
            if (article == null) return BadRequest();

            var db = GetDbContext(continent);
            db.Set<Article>().Add(article);
            await db.SaveChangesAsync();

            var dto = new CacheService.Dtos.ArticleDto
            {
                Id = article.Id.ToString(),
                Title = article.Title,
                Content = article.Content,
                Author = article.Author,
                PublishedAt = article.PublishedAt
            };

            await _cacheService.SetArticleAsync(continent.ToLower(), dto);
            return CreatedAtAction(nameof(GetById), new { id = article.Id, continent }, article);
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
                Id = existingArticle.Id.ToString(),
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

            await _cacheService.RemoveArticleAsync(continent.ToLower(), article.Id.ToString());
            return NoContent();
        }
    }
}
