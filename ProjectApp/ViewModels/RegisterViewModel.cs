using System.ComponentModel.DataAnnotations;
using System.Security.Policy;

namespace ProjectApp.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 25 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, and characters . _ -")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
        public string Password { get; set; }

        [Required]
        public string UserType { get; set; } // Buyer or Seller
    }
}

