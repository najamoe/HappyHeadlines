using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using PublisherService.Clients;
using PublisherService.Infrastructure;
using PublisherService.Models;
using System.Diagnostics;

[ApiController]
[Route("[controller]")]
public class PublishController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private static readonly ActivitySource ActivitySource = new("PublisherService");
    private readonly HttpClient _profanityClient;

    public PublishController(
        RabbitMqPublisher publisher,
        IHttpClientFactory httpClientFactory)
    {
        _publisher = publisher;
        _profanityClient = httpClientFactory.CreateClient("ProfanityService");
    }

    [HttpPost("{draftId}")]
    public async Task<IActionResult> Publish(int draftId, [FromServices] DraftClient draftClient)
    {
        using var activity = ActivitySource.StartActivity("PublishArticle");

        // 1. Fetch draft
        var draft = await draftClient.GetDraftAsync(draftId);
        if (draft == null)
        {
            return NotFound(new { message = "Draft not found" });
        }

        if (draft.Status != "ReadyToPublish")
        {
            return BadRequest(new { message = "Draft is not ready to publish" });
        }

        // 2. Check profanity
        var response = await _profanityClient.PostAsJsonAsync("api/profanity/check", new
        {
            text = draft.Content
        });

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new { message = "ProfanityService unavailable" });
        }

        var check = await response.Content.ReadFromJsonAsync<ProfanityCheckResponse>();
        if (check == null || !check.IsClean)
        {
            return BadRequest(new { message = "Draft contains profanity" });
        }

        // 3. Publish article
        var article = new ArticleDto
        {
            Id = draft.Id.ToString(),
            Title = draft.Title,
            Content = draft.Content,
            TraceId = activity?.TraceId.ToString() ?? Guid.NewGuid().ToString()
        };

        await _publisher.PublishArticleAsync(article);

        return Ok(new { status = "published", traceId = article.TraceId });
    }

    // DTO to match ProfanityService response
    public class ProfanityCheckResponse
    {
        public bool IsClean { get; set; }
    }
}
