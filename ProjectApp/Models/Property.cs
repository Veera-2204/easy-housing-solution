using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectApp.Models

{

    public class Property

    {

        [Key]

        public int PropertyId { get; set; }

        [Required]

        [MaxLength(50)]

        public string PropertyName { get; set; }

        [Required]

        [MaxLength(15)]

        public string PropertyType { get; set; } // Example: Apartment, Villa, etc.

        [Required]

        [MaxLength(10)]

        public string PropertyOption { get; set; } // Options: Sell or Rent

        [MaxLength(250)]

        public string Description { get; set; }

        [Required]

        [MaxLength(250)]

        public string Address { get; set; }

        [Required]

        [Column(TypeName = "money")]

        public decimal PriceRange { get; set; }

        [Column(TypeName = "money")]

        public decimal? InitialDeposit { get; set; }

        [MaxLength(25)]

        public string Landmark { get; set; }

        [Required]

        public bool IsActive { get; set; }

        // Foreign Key to Seller

        [ForeignKey("Seller")]

        public int SellerId { get; set; }

        public Seller Seller { get; set; } // Navigation property

        // Foreign Key to City

        [ForeignKey("City")]

        public int? CityId { get; set; } 

        public City City { get; set; } // Navigation property

        // Navigation Property for related images

        public ICollection<Image> Images { get; set; } = new List<Image>();

    }

}

