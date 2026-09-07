using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectApp.ViewModels
{
    public class BuyerViewModel
    {
        public int BuyerId { get; set; }  // This is optional for creating, but included here if needed for updates

        [Required]
        [MaxLength(25)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }  // FirstName is required and has a max length of 25

        [MaxLength(25)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }  // LastName is optional with a max length of 25

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }  // DateOfBirth is required

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits.")]
        [Display(Name = "Phone Number")]
        public string PhoneNo { get; set; }  // PhoneNo is required, max length 10, and must be 10 digits

        public string? EmailId { get; set; }  // EmailId is required with max length 50


    }
}