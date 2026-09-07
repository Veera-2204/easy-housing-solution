using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectApp.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        // Foreign Key to Buyer
        [ForeignKey("Buyer")]
        public int BuyerId { get; set; }
        public Buyer Buyer { get; set; } // Navigation property to Buyer

        // Foreign Key to Property
        [ForeignKey("Property")]
        public int PropertyId { get; set; }
        public Property Property { get; set; } // Navigation property to Property
    }
}
