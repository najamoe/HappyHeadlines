using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using PublisherService.Models;
using PublisherService.Infrastructure;

[ApiController]
[Route("[controller]")]
public class PublishController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private static readonly ActivitySource ActivitySource = new("PublisherService");

    public PublishController(RabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] ArticleDto article)
    {
        using var activity = ActivitySource.StartActivity("PublishArticle");

        article.TraceId ??= activity?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        await _publisher.PublishArticleAsync(article); // async call
        return Ok(new { status = "published", traceId = article.TraceId });
    }

}
