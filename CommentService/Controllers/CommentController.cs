using Microsoft.AspNetCore.Mvc;
using CommentService.Data;
using CommentService.Models;
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
        private readonly HttpClient _httpClient;

        public CommentController(CommentDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;

            // Create a named HttpClient specifically for ProfanityService
            _httpClient = httpClientFactory.CreateClient("ProfanityService");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Comment comment)
        {
            if (comment == null)
                return BadRequest();

            // Serialize comment text to JSON
            var json = JsonSerializer.Serialize(new { text = comment.Text });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/api/profanity/check", content);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Error checking profanity");

                var result = await response.Content.ReadAsStringAsync();
                var check = JsonSerializer.Deserialize<ProfanityCheckResult>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (check != null && !check.isClean)
                {
                    return BadRequest("Comment contains profanity");
                }
                // Save comment to database
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(Create), new { id = comment.Id }, comment);

            }
            catch (BrokenCircuitException)
            {
                return StatusCode(503, "Profanity service is unavailable. Please try again later.");
            }
        }


        // DTO for deserialization

        public class ProfanityCheckResult
        {
            public bool isClean { get; set; }
        }
    }
}
