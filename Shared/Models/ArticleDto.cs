namespace Shared.Models
{
    public class ArticleDto
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string Author { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        //  sourceArticleID for GlobalDB copies to trace origin
        public int? SourceArticleId { get; set; }

        public string? SourceContinent { get; set; }
        public required string Continent { get; set; } = "Global"; 

        // Added for distributed tracing (Zipkin / OpenTelemetry)
        public required string TraceId { get; set; }
    }
}
