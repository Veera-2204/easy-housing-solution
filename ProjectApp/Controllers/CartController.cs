using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Import logging namespace
using ProjectApp.Data;
using ProjectApp.Models;
using System.Linq;

namespace ProjectApp.Controllers
{
    /// <summary>
    /// This class is used to implement cart functionalities by viewing properties added to cart, remove property from cart and go back to 
    /// adding more properties
    /// </summary>
    public class CartController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly ILogger<CartController> _logger; // Add logger

        public CartController(AppsDbContext context, ILogger<CartController> logger) // Inject logger
        {
            _context = context;
            _logger = logger; // Assign logger
        }

        public IActionResult ViewCart(int buyerId)
        {
            _logger.LogInformation("ViewCart action called for Buyer ID: {BuyerId}", buyerId); // Log method access
            ViewBag.BuyerId = buyerId;
            // Fetch cart items for the current buyer with images included
            var cartItems = _context.Carts
                .Include(c => c.Property)      // Include property details
                .ThenInclude(p => p.Images)    // Include property images
                .Include(c => c.Property.Seller) // Include seller details
                .Include(c => c.Property.City) // Include city details
                .ThenInclude(city => city.State) // Include state details
                .Where(c => c.BuyerId == buyerId) // Filter cart items by buyer ID
                .ToList();

            if (cartItems == null || !cartItems.Any())
            {
                _logger.LogInformation("Cart is empty for Buyer ID: {BuyerId}", buyerId); // Log empty cart scenario
                return View("EmptyCart", buyerId); // You can create an EmptyCart view if the cart is empty
            }

            _logger.LogInformation("Returning {CartItemCount} items to the ViewCart view for Buyer ID: {BuyerId}", cartItems.Count, buyerId); // Log cart items count
            return View(cartItems); // Return the cart items to the ViewCart view
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int cartId, int buyerId)
        {
            _logger.LogInformation("RemoveFromCart action called for Cart ID: {CartId}, Buyer ID: {BuyerId}", cartId, buyerId); // Log method access

            // Find the cart item by CartId
            var cartItem = _context.Carts
                .Include(c => c.Property) // Ensure Property details are loaded
                .FirstOrDefault(c => c.CartId == cartId);

            if (cartItem == null)
            {
                _logger.LogWarning("Cart item not found for Cart ID: {CartId}", cartId); 
                return NotFound(); // Cart item not found
            }

            _context.Carts.Remove(cartItem);
            _context.SaveChanges();

            _logger.LogInformation("Cart item removed successfully for Cart ID: {CartId}, Buyer ID: {BuyerId}", cartId, buyerId); // Log successful removal
            return RedirectToAction("ViewCart", new { buyerId = buyerId });
        }
    }
}
