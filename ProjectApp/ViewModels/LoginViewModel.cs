using System.ComponentModel.DataAnnotations;

namespace ProjectApp.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [StringLength(25)]
        public string UserName { get; set; }

        

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
