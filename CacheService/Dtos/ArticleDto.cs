using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheService.Dtos
{
    public class ArticleDto
    {
        public int Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string Author { get; set; } = default!;
        public DateTime PublishedAt { get; set; }
    }
}
