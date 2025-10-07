using CommentService.Data;
using CommentService.Models;
using CacheService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Polly.CircuitBreaker;

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

        // -------------------- GET --------------------
        [HttpGet("{articleId}")]
        public async Task<IActionResult> GetCommentsByArticleId(int articleId)
        {
            if (articleId <= 0)
                return BadRequest("Invalid article ID.");

            // Try cache first
            var cachedComments = await _commentCacheService.GetCommentsAsync(articleId);
            if (cachedComments != null)
            {
                _logger.LogInformation("Cache HIT for comments of article {ArticleId}", articleId);
                return Ok(cachedComments);
            }

            _logger.LogInformation("Cache MISS for comments of article {ArticleId}", articleId);

            // If its a cash miss it fetches from DB
            var dbComments = await _context.Comments
                .Where(c => c.ArticleId == articleId)
                .ToListAsync();

            if (!dbComments.Any())
                return NotFound($"No comments found for article {articleId}");

            // Store in cache (will trigger LRU eviction if needed)
            await _commentCacheService.SetCommentsAsync(
            articleId,
                dbComments.Select(c => new CacheService.Dtos.CommentDto
                {
                Id = c.Id,
                ArticleId = c.ArticleId,
                Text = c.Text,
                Author = c.Author,
                CreatedAt = DateTime.UtcNow             
                }).ToList()
                );
            return Ok(dbComments);
        }

        // -------------------- POST --------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string continent, [FromBody] Comment comment)
        {
            if (comment == null)
                return BadRequest("Comment is null.");

            if (string.IsNullOrWhiteSpace(continent))
                return BadRequest("Continent is required.");

            // Check if the article exists
            var articleResponse = await _articleClient.GetAsync($"/article/{comment.ArticleId}?continent={continent}");
            if (!articleResponse.IsSuccessStatusCode)
                return BadRequest("Article not found on the specified continent.");

            // Profanity check
            var json = JsonSerializer.Serialize(new { text = comment.Text });
            _logger.LogInformation("Sending JSON to ProfanityService: {Json}", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _profanityClient.PostAsync("/api/profanity/check", content);
                _logger.LogInformation("ProfanityService responded with status code: {StatusCode}", response.StatusCode);

                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("ProfanityService response body: {ResponseBody}", result);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error checking profanity: {StatusCode}", response.StatusCode);
                    return StatusCode((int)response.StatusCode, "Error checking profanity.");
                }

                var check = JsonSerializer.Deserialize<ProfanityCheckResult>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (check != null && !check.isClean)
                {
                    _logger.LogWarning("Comment contains profanity: {CommentText}", comment.Text);
                    return BadRequest("Comment contains profanity.");
                }

                // Save comment
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Optional: remove from cache so new comment is loaded next time
                await _commentCacheService.RemoveCommentsAsync(comment.ArticleId);

                _logger.LogInformation("Comment saved successfully with ID {CommentId}", comment.Id);
                return CreatedAtAction(nameof(Create), new { id = comment.Id }, comment);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Profanity service is unavailable due to circuit breaker.");
                return StatusCode(503, "Profanity service is unavailable. Please try again later.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error connecting to ProfanityService.");
                return StatusCode(503, "Error connecting to ProfanityService.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize ProfanityService response.");
                return StatusCode(500, "Invalid response from ProfanityService.");
            }
        }

        public class ProfanityCheckResult
        {
            public bool isClean { get; set; }
        }
    }
}
