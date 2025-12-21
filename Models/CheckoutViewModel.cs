using System.ComponentModel.DataAnnotations;

namespace Coursify.Models
{
    public class CheckoutViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public decimal Price { get; set; }
        public string TeacherName { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Expiry month is required")]
        [Range(1, 12, ErrorMessage = "Invalid month")]
        public int ExpiryMonth { get; set; }

        [Required(ErrorMessage = "Expiry year is required")]
        [Range(2024, 2099, ErrorMessage = "Invalid year")]
        public int ExpiryYear { get; set; }

        [Required(ErrorMessage = "CVV is required")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits")]
        public string CVV { get; set; }

        [Required(ErrorMessage = "Cardholder name is required")]
        public string CardholderName { get; set; }
    }
}

