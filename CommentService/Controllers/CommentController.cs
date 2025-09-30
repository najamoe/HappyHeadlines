using CommentService.Data;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        public CommentController(CommentDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _articleClient = httpClientFactory.CreateClient("ArticleService");
            _profanityClient = httpClientFactory.CreateClient("ProfanityService");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string continent, [FromBody] Comment comment)
        {
            if (comment == null)
                return BadRequest("Comment is null.");

            if (string.IsNullOrWhiteSpace(continent))
                return BadRequest("Continent is required.");

            // Check if the article exists on the specified continent
            var articleResponse = await _articleClient.GetAsync($"/article/{comment.ArticleId}?continent={continent}");
            if (!articleResponse.IsSuccessStatusCode)
                return BadRequest("Article not found on the specified continent.");

            // Check for profanity
            var json = JsonSerializer.Serialize(new { text = comment.Text });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _profanityClient.PostAsync("/api/profanity/check", content);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Error checking profanity.");

                var result = await response.Content.ReadAsStringAsync();
                var check = JsonSerializer.Deserialize<ProfanityCheckResult>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (check != null && !check.isClean)
                    return BadRequest("Comment contains profanity.");

                // Save comment
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(Create), new { id = comment.Id }, comment);
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(503, "Profanity service is unavailable. Please try again later.");
            }
        }

        // DTO for profanity check response
        public class ProfanityCheckResult
        {
            public bool isClean { get; set; }
        }
    }
}
