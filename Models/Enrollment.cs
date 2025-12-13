using coursify.Models;
using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public decimal AmountPaid { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public double ProgressPercentage { get; set; } = 0;
        public bool HasCertificate { get; set; } = false;
        public DateTime? CompletedDate { get; set; }

        // Relationships
        //1 student hi enroll hoga
        public User Student { get; set; }
        //1 course ki enrollment hy
        public Course Course { get; set; }

        public List<Progress> Progresses { get; set; } = new();
        public Certificate? Certificate { get; set; }
    }
}