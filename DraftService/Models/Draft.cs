using System;
using System.ComponentModel.DataAnnotations;

namespace DraftService.Models
{
    public enum DraftStatus
    {
        Draft,
        ReadyToPublish
    }

    public class Draft
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        [MaxLength(100)]
        public string Author { get; set; }

        public DraftStatus Status { get; set; } = DraftStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
