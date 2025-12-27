using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; } // Foreign key to Module

        [Required]
        public string Title { get; set; } // Required title

        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? ContentUrl { get; set; }
        public int? Duration { get; set; } // Made nullable to avoid validation errors if empty
        public int? OrderNo { get; set; } // Made nullable to avoid silent failure
        public bool IsPreview { get; set; } = false;

        // ✅ Changed to UTC to fix PostgreSQL timestamp issue
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Relationships
        public Module? Module { get; set; } // Nullable to avoid ModelState error
    }
}
