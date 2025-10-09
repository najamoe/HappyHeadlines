
namespace Shared.Models
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string? Author { get; set; }
        public string? Text { get; set; }
        public int ArticleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
