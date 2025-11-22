using CacheService.Services;
using CommentService.Data;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Text;
using System.Text.Json;

namespace CommentService.Controllers
{
    [Route("api/comments")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly CommentDbContext _context;
        private readonly HttpClient _articleClient;
        private readonly HttpClient _profanityClient;
        private readonly ILogger<CommentController> _logger;
        private readonly CommentCacheService _commentCacheService;

        public CommentController(
            CommentDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<CommentController> logger,
            CommentCacheService commentCacheService)
        {
            _context = context;
            _articleClient = httpClientFactory.CreateClient("ArticleService");
            _profanityClient = httpClientFactory.CreateClient("ProfanityService");
            _logger = logger;
            _commentCacheService = commentCacheService;
        }

        // GET /api/comments/{continent}/{articleId}
        [HttpGet("{continent}/{articleId}")]
        public async Task<IActionResult> Get([FromRoute] string continent, [FromRoute] int articleId)
        {
            if (string.IsNullOrWhiteSpace(continent))
                return BadRequest("Continent is required.");

            var cachedComments = await _commentCacheService.GetCommentsAsync(continent, articleId);
            if (cachedComments != null && cachedComments.Any())
            {
                _logger.LogInformation("Cache HIT for article {ArticleId} in {Continent}", articleId, continent);
                return Ok(cachedComments);
            }

            _logger.LogInformation("Cache MISS for article {ArticleId} in {Continent}", articleId, continent);

            var dbComments = await _context.Comments
                .Where(c => c.ArticleId == articleId && c.Continent == continent)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Author = c.Author,
                    Text = c.Text,
                    ArticleId = c.ArticleId,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            if (dbComments == null || dbComments.Count == 0)
                return NotFound("No comments found for this article.");

            await _commentCacheService.SetCommentsAsync(continent, articleId, dbComments);
            return Ok(dbComments);
        }

        // POST /api/comments/{continent}
        [HttpPost("{continent}")]
        public async Task<IActionResult> Create([FromRoute] string continent, [FromBody] CommentDto dto)
        {
            if (string.IsNullOrWhiteSpace(continent))
                return BadRequest("Continent is required.");

            // Checking if the article exists in ArticleService
            var articleResp = await _articleClient.GetAsync($"/article/{dto.ArticleId}?continent={continent}");
            if (!articleResp.IsSuccessStatusCode)
                return BadRequest("Article not found on the specified continent.");

            // prepare payload for ProfanityService
            var payload = JsonSerializer.Serialize(new { text = dto.Text });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            // call ProfanityService to check for profanity with Circuit Breaker
            try
            {
                var resp = await _profanityClient.PostAsync("/api/profanity/check", content);

                if (!resp.IsSuccessStatusCode)
                    return StatusCode((int)resp.StatusCode, "Error checking profanity.");

                var check = JsonSerializer.Deserialize<ProfanityCheckResult>(
                    await resp.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (check is { isClean: false })
                    return BadRequest("Comment contains profanity.");
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException)
            {
                // Circuit breaker is open - reject the comment
                _logger.LogWarning("ProfanityService unavailable – rejecting comment due to circuit breaker");

                return StatusCode(503, new { message = "Cannot check profanity - ProfanityService unavailable. Try again later." });
            }
            catch (HttpRequestException ex)
            {
                // Other HTTP request errors - deny the comment
                _logger.LogError(ex, "ProfanityService request failed");
                return StatusCode(503, new { message = "Cannot check profanity - ProfanityService unavailable. Try again later." });
            }


            // save comment to database
            var entity = new Comment
            {
                Author = dto.Author,
                Text = dto.Text,
                ArticleId = dto.ArticleId,
                Continent = continent,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(entity);
            await _context.SaveChangesAsync();

            // update cache
            await _commentCacheService.RemoveCommentsAsync(continent, entity.ArticleId);

            var result = new CommentDto
            {
                Id = entity.Id,
                Author = entity.Author,
                Text = entity.Text,
                ArticleId = entity.ArticleId,
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtAction(nameof(Create), new { continent, id = result.Id }, result);
        }


        private class ProfanityCheckResult
        {
            public bool isClean { get; set; }
        }
    }
}
