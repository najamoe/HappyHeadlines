namespace PublisherService.Models
{

    public class ArticleDto
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public string Author { get; set; } = default!;
        public DateTime PublishedAt { get; set; }
        public required string TraceId { get; set; }
    }

}
