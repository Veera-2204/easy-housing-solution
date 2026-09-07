using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProjectApp.Models
{
    public class Buyer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BuyerId { get; set; }  // Primary Key, Identity column

        [Required]
        [MaxLength(25)]
        public string FirstName { get; set; }  // FirstName is required and has a max length of 25

        [MaxLength(25)]
        public string LastName { get; set; }  // LastName is optional with a max length of 25

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }  // DateOfBirth is required

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits.")]
        public string PhoneNo { get; set; }  // PhoneNo is required, max length 10, and must be 10 digits

        [Required]
        [MaxLength(50)]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string EmailId { get; set; }  // EmailId is required with max length 50

        // Foreign key to IdentityUser
        //[Key]
        [Required]
        public string? UserId { get; set; } // Foreign key property

        [ForeignKey("UserId")]
        public virtual IdentityUser? AppsUser { get; set; } // Navigation property to IdentityUser
    }
}
