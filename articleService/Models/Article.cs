using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ArticleService.Models
{
    public class Article
    {
        public int Id { get; set; }
        public required string Author { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }

    }
}