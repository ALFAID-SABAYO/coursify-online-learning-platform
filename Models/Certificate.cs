using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        public string CertificateUrl { get; set; }

        public string UniqueCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 10);
        public DateTime IssuedDate { get; set; } = DateTime.Now;

        // Relationship
        public Enrollment Enrollment { get; set; }
    }
}