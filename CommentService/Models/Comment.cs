using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;


namespace CommentService.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string? Author { get; set; }
        public string? Text { get; set; }
        public int ArticleId { get; set; }
    }
}
