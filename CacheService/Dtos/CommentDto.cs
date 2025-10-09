using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheService.Dtos
{
    public class CommentDto
    {
        public int Id { get; set; } = default!;
        public int ArticleId { get; set; } = default!;
        public string Text { get; set; } = default!;
        public string Author { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
