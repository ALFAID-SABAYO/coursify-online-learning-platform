using coursify.Models;
using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class Progress
    {
        public int Id { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedDate { get; set; }
        public DateTime LastAccessed { get; set; } = DateTime.Now;

        // Relationships
        public Enrollment Enrollment { get; set; }
        public Lesson Lesson { get; set; }
    }
}