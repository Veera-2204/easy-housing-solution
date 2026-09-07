using Microsoft.AspNetCore.Mvc.Rendering;
using ProjectApp.Models;

namespace ProjectApp.ViewModels
{
    public class PropertyViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public string PropertyOption { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal PriceRange { get; set; }
        public decimal InitialDeposit { get; set; }
        public string Landmark { get; set; }
        public bool IsActive { get; set; }
        public int SellerId { get; set; }
        public string? StateName { get; set; }
        public string? CityName { get; set; }
        public int? CityId { get; set; }
        public int StateId { get; set; }
        public string? SellerEmailId { get; set; }

        // Properties for dropdowns
        public SelectList? States { get; set; }
        public SelectList? Cities { get; set; }
        public IEnumerable<SelectListItem>? PropertyOptions { get; set; }
        public ICollection<Image> Images { get; set; } = new List<Image>();
        public List<string>? ImageBase64Strings { get; set; } = new List<string>();

    }
}