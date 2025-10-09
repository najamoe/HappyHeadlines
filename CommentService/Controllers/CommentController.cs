using CacheService.Services;
using CommentService.Data;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Text;
using System.Text.Json;

// Aliases to avoid "Comment" ambiguity
using CommentEntity = CommentService.Models.Comment;
using CommentDto = Shared.Models.CommentDto;

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

        // POST /api/comments/{continent}
        [HttpPost("{continent}")]
        public async Task<IActionResult> Create([FromRoute] string continent, [FromBody] CommentDto commentDto)
        {
            if (string.IsNullOrWhiteSpace(continent))
                return BadRequest("Continent is required.");

            // Check if article exists
            var articleResp = await _articleClient.GetAsync($"/article/{commentDto.ArticleId}?continent={continent}");
            if (!articleResp.IsSuccessStatusCode)
                return BadRequest("Article not found on the specified continent.");

            // Profanity check
            var payload = JsonSerializer.Serialize(new { text = commentDto.Text });
            var resp = await _profanityClient.PostAsync("/api/profanity/check",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, "Error checking profanity.");

            var check = JsonSerializer.Deserialize<ProfanityCheckResult>(
                await resp.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (check is { isClean: false })
                return BadRequest("Comment contains profanity.");

            // Map DTO to entity and set continent automatically
            var entity = new CommentEntity
            {
                Author = commentDto.Author,
                Text = commentDto.Text,
                ArticleId = commentDto.ArticleId,
                Continent = continent,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(entity);
            await _context.SaveChangesAsync();

            await _commentCacheService.RemoveCommentsAsync(continent, entity.ArticleId);

            // Map back to DTO for response
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

        public class ProfanityCheckResult
        {
            public bool isClean { get; set; }
        }
    }
}
