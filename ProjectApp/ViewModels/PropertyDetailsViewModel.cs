using System.Collections.Generic;
using ProjectApp.Models;

namespace ProjectApp.ViewModels
{
    public class PropertyDetailsViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; }
        public string PropertyType { get; set; } // Added PropertyType for seller view
        public string PropertyOption { get; set; }
        public decimal? InitialDeposit { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string Landmark { get; set; } // Added Landmark
        public string CityName { get; set; } // Added CityName
        public string StateName { get; set; } // Added StateName
        public decimal Price { get; set; }
        public bool IsActive { get; set; } // Added IsActive for seller view
        public string SellerName { get; set; }
        public string SellerContactInfo { get; set; }
        public string SellerEmailId { get; set; }
        public List<Image> Images { get; set; } = new List<Image>();
        public int? BuyerId { get; internal set; }
    }
}
