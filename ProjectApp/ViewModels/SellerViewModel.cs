using ProjectApp.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace ProjectApp.ViewModels
{
    public class SellerViewModel
    {
        [Required]
        [MaxLength(25)]
        public string UserName { get; set; }

        [Required]
        [MaxLength(25)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [MaxLength(25)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }


        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits.")]
        [Display(Name = "Phone Number")]
        public string PhoneNo { get; set; }

        [Required]
        [MaxLength(250)]
        public string Address { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmailId { get; set; }

        [Required]
        public string StateName { get; set; }

        [Required]
        public string CityName { get; set; }
    }
}