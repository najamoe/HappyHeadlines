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
    private readonly DraftClient _draftClient;

    public PublishController(
        RabbitMqPublisher publisher,
        IHttpClientFactory httpClientFactory,
        DraftClient draftClient)
    {
        _publisher = publisher;
        _profanityClient = httpClientFactory.CreateClient("ProfanityService");
        _draftClient = draftClient;
    }

    [HttpPost("{draftId}")]
    public async Task<IActionResult> Publish(int draftId)
    {
        using var activity = ActivitySource.StartActivity("PublishArticle");

        // 1. Fetch draft
        var draft = await _draftClient.GetDraftAsync(draftId);
        if (draft == null)
        {
            return NotFound(new { message = "Draft not found" });
        }

        // 2. Check draft status
        if (!string.Equals(draft.Status, "ReadyToPublish", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Draft is not ready to publish" });
        }

        // 3. Call ProfanityService
        var response = await _profanityClient.PostAsJsonAsync("api/profanity/check", new
        {
            Text = draft.Content
        });

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new { message = "Profanity service unavailable" });
        }

        var result = await response.Content.ReadFromJsonAsync<ProfanityResponse>();
        if (result is null || !result.IsClean)
        {
            return BadRequest(new { message = "Draft contains profanity" });
        }

        // 4. Map draft → article and publish to queue
        var article = new ArticleDto
        {
            Id = draft.Id.ToString(),
            Title = draft.Title,
            Content = draft.Content,
            TraceId = activity?.TraceId.ToString() ?? Guid.NewGuid().ToString()
        };

        await _publisher.PublishArticleAsync(article);

        // 5. Respond with success
        return Ok(new { status = "published", traceId = article.TraceId });
    }

    public class ProfanityResponse
    {
        public bool IsClean { get; set; }
    }
}