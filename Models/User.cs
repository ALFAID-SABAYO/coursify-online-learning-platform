using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // "Student", "Teacher", "Admin"

        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Relationships
        //1 to many aik student ya teacher kay pass bohut say courses ho sakty hy
        public List<Course> Courses { get; set; } = new();
        //1 student bohut say courses may enroll hosakta hy
        public List<Enrollment> Enrollments { get; set; } = new();
    }
}