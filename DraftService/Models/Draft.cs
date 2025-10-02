using System;
using System.ComponentModel.DataAnnotations;

namespace DraftService.Models
{
    public enum DraftStatus
    {
        Draft, // 0
        ReadyToPublish // 1
    }

    public class Draft
    {
        [Key]
        public int Id { get; set; }

      
        [MaxLength(200)]
        public required string Title { get; set; }

  
        public required string Content { get; set; }

     
        [MaxLength(100)]
        public required string Author { get; set; }

        public DraftStatus Status { get; set; } = DraftStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
