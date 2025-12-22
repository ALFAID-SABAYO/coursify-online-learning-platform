using coursify.Models;
using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class Module
    {
       
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }
       
        public int OrderNo { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Relationships
        public Course Course { get; set; }
        public List<Lesson> Lessons { get; set; } = new();
    }
}