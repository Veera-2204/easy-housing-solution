using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Data;
using ProjectApp.Models;
using ProjectApp.ViewModels;

namespace ProjectApp.Controllers
{
    /// <summary>
    /// This class is used to display details for a specific property (along with seller contact details)
    /// We can either add this property to cart or go back to respective property view
    /// </summary>
    public class SpecificPropertyController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly ILogger<SpecificPropertyController> _logger;

        public SpecificPropertyController(AppsDbContext context, ILogger<SpecificPropertyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /SpecificProperty/Details/5
        public IActionResult Details(int propertyId, int? buyerId)
        {
            _logger.LogInformation("Fetching details for PropertyId: {PropertyId}", propertyId);

            var property = _context.Properties
                 .Include(p => p.Seller)
                 .Include(p => p.Images)
                 .Include(p => p.City) 
                 .ThenInclude(c => c.State) 
                 .FirstOrDefault(p => p.PropertyId == propertyId);

            if (property == null)
            {
                _logger.LogWarning("Property not found with PropertyId: {PropertyId}", propertyId);
                return NotFound();
            }

            ViewBag.BuyerId = buyerId;

            var viewModel = new PropertyDetailsViewModel
            {
                PropertyId = property.PropertyId,
                PropertyName = property.PropertyName,
                PropertyType = property.PropertyType,
                PropertyOption = property.PropertyOption,
                InitialDeposit = property.InitialDeposit,
                Description = property.Description,
                Address = property.Address,
                Landmark = property.Landmark,
                CityName = property.City?.CityName,
                StateName = property.City?.State?.StateName,
                Price = property.PriceRange,
                SellerName = property.Seller?.FirstName,
                SellerContactInfo = property.Seller?.PhoneNo,
                SellerEmailId = property.Seller?.EmailId,
                Images = property.Images.ToList(),
                BuyerId = buyerId // Add this to your ViewModel if needed
            };



            _logger.LogInformation("Details fetched for PropertyId: {PropertyId}", propertyId);
            return View("Details", viewModel);
        }

        [HttpPost]
        public IActionResult AddToCart(int propertyId, int buyerid)
        {
            _logger.LogInformation("Attempting to add PropertyId: {PropertyId} to cart for BuyerId: {BuyerId}", propertyId, buyerid);

            var property = _context.Properties.FirstOrDefault(p => p.PropertyId == propertyId);
            var buyer = _context.Buyers.FirstOrDefault(b => b.BuyerId == buyerid);

            if (property != null && buyer != null)
            {
                // Check if the property is already in the buyer's cart
                var existingCartItem = _context.Carts
                    .FirstOrDefault(c => c.BuyerId == buyerid && c.PropertyId == propertyId);

                if (existingCartItem != null)
                {
                    _logger.LogWarning("PropertyId: {PropertyId} already exists in cart for BuyerId: {BuyerId}", propertyId, buyerid);
                    TempData["ErrorMessage"] = "This property is already in your cart.";
                    return RedirectToAction("ViewCart", "Cart", new { buyerId = buyerid });
                }

                var cart = new Cart
                {
                    BuyerId = buyerid,
                    Buyer = buyer,
                    PropertyId = propertyId,
                    Property = property
                };

                _context.Carts.Add(cart);
                _context.SaveChanges();
                _logger.LogInformation("PropertyId: {PropertyId} added to cart for BuyerId: {BuyerId}", propertyId, buyerid);
                TempData["SuccessMessage"] = "Property added to cart successfully!";
            }
            else
            {
                _logger.LogWarning("Property or Buyer not found - PropertyId: {PropertyId}, BuyerId: {BuyerId}", propertyId, buyerid);
                TempData["ErrorMessage"] = "Unable to add property to cart. Please try again.";
            }

            return RedirectToAction("ViewCart", "Cart", new { buyerId = buyerid });
        }
    }
}
