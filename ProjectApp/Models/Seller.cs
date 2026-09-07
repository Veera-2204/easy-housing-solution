using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProjectApp.Models
{
    public class Seller
    {
        [Key]
        public int SellerId { get; set; }

        [Required]
        [MaxLength(25)]
        public string UserName { get; set; }

        [Required]
        [MaxLength(25)]
        public string FirstName { get; set; }

        [MaxLength(25)]
        public string LastName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [MaxLength(10)]
        [Phone]
        public string PhoneNo { get; set; }

        [Required]
        [MaxLength(250)]
        public string Address { get; set; }

        // Ensure this matches your database schema and model requirements
        public int StateId { get; set; }

        [ForeignKey("StateId")]
        public State State { get; set; }

        // Foreign Key to City
        public int CityId { get; set; }
        [ForeignKey("CityId")]
        public City City { get; set; } // Navigation property

        [Required]
        [MaxLength(50)]
        [EmailAddress]
        public string EmailId { get; set; }

        // Foreign key to IdentityUser
        
        [Required]
        public string? UserId { get; set; } // Foreign key property
       
        [ForeignKey("UserId")]
        public virtual IdentityUser? AppsUser { get; set; } // Navigation property to IdentityUser

        // Navigation Property for related properties
        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}
