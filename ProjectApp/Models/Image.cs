using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectApp.Models
{
    public class Image
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        
        public byte[] ImageData { get; set; }

        // Foreign Key to Property
        [ForeignKey("Property")]
        public int PropertyId { get; set; }

        public Property Property { get; set; } // Navigation property
    }
}
