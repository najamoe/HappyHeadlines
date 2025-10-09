using System;

namespace ProfanityService.Models
{
    public class Profanity
    {
        public int Id { get; set; }
        public string? Word { get; set; }
    }

    public class ProfanityCheckRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}
