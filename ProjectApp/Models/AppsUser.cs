using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ProjectApp.Models
{
    public class AppsUser : IdentityUser
    {

        //[MaxLength(25)]
        //public string UserName { get; set; }


        //[Required]
        //[MaxLength(25)]
        //public string Password { get; set; }

        // Custom property to differentiate user roles/types.
        [Required]
        [MaxLength(15)]
        public string UserType { get; set; }  // Admin, Buyer, or Seller
    }
}
