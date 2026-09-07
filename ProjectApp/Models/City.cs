using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectApp.Models
{
    public class City
    {
        [Key]
        public int CityId { get; set; }

        [Required]
        [MaxLength(30)]
        public string CityName { get; set; }

        [ForeignKey("State")]
        public int StateId { get; set; } // Foreign key property to link to State

        // Navigation Property to State
        public State State { get; set; } // Navigation property to the State entity

        // Navigation Property to Properties
        public ICollection<Property> Properties { get; set; } = new List<Property>(); // Collection of Properties related to this City
    }
}
