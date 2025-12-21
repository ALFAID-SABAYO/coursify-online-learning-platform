using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Coursify.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int TeacherId { get; set; }

        public string? ThumbnailUrl { get; set; }
        
        // Main course video URL (YouTube link or direct video URL)
        // Students will watch this video after purchasing the course
        public string? PreviewVideoUrl { get; set; }
        public bool IsPublished { get; set; } = false;
        public string Level { get; set; } = "Beginner";
        public string Language { get; set; } = "English";
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        
        // 1 course 1 hi teacher parhyga  
        public User Teacher { get; set; }
        //single course may kafi students enroll hongy
        public List<Enrollment> Enrollments { get; set; } = new();
        //single course may kafi module howakty hy
        public List<Module> Modules { get; set; } = new();
    }
}