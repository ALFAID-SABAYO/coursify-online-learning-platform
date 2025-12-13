using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? ContentUrl { get; set; }
        public int? Duration { get; set; }
        public int OrderNo { get; set; }
        public bool IsPreview { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Relationship
        public Module Module { get; set; }
    }
}