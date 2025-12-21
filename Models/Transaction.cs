using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        public int RecipientId { get; set; } // Teacher or Admin User ID

        [Required]
        public string RecipientRole { get; set; } // "Teacher" or "Admin"

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public decimal Percentage { get; set; } // 5% for Admin, 95% for Teacher

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Relationships
        public Enrollment Enrollment { get; set; }
        public User Recipient { get; set; }
    }
}

