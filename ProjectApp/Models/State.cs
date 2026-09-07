

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectApp.Models
{
    public class State
    {
        [Key]
        public int StateId { get; set; }

        [Required]
        [MaxLength(30)]
        public string StateName { get; set; }

        // Navigation Property
        public ICollection<Seller> Sellers { get; set; }
        public ICollection<City> Cities { get; set; } = new List<City>();
    }
}
