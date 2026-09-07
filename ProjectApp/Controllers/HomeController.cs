using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Import logging namespace
using ProjectApp.Data;
using ProjectApp.Models;

namespace ProjectApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppsDbContext _context;
        private readonly ILogger<HomeController> _logger; // Add logger

        public HomeController(UserManager<IdentityUser> userManager, AppsDbContext context, ILogger<HomeController> logger) // Inject logger
        {
            _userManager = userManager;
            _context = context;
            _logger = logger; // Assign logger
        }

        /// <summary>
        /// The Index method is used to display the Homepage of the application
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Index action called."); // Log method access

            // Fetch the current user
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // Fetch user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Redirect to respective dashboard based on the role
                if (roles.Contains("Admin"))
                {
                    _logger.LogInformation("Redirecting Admin user to AdminDashboard."); // Log redirection
                    return RedirectToAction("AdminDashboard", "Admin");
                }
                else if (roles.Contains("Seller"))
                {
                    var seller = await _context.Sellers
                                .FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (seller != null)
                    {
                        _logger.LogInformation("Redirecting Seller user to SellerDashboard with SellerId: {SellerId}", seller.SellerId); // Log redirection
                        return RedirectToAction("SellerDashboard", "Seller", new { sellerId = seller.SellerId });
                    }
                    _logger.LogWarning("Seller not found for UserId: {UserId}", user.Id); // Log warning if seller is not found
                }
                else if (roles.Contains("Buyer"))
                {
                    var buyer = await _context.Buyers
                               .FirstOrDefaultAsync(b => b.UserId == user.Id);
                    if (buyer != null)
                    {
                        _logger.LogInformation("Redirecting Buyer user to BuyerDashboard with BuyerId: {BuyerId}", buyer.BuyerId); // Log redirection
                        return RedirectToAction("BuyerDashboard", "Buyer", new { buyerId = buyer.BuyerId });
                    }
                    _logger.LogWarning("Buyer not found for UserId: {UserId}", user.Id); // Log warning if buyer is not found
                }
            }

            // Fetch properties if no specific role
            var properties = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.Seller)
                .Where(p => p.IsActive)
                .ToListAsync();

            // Fetch all images related to properties if needed
            var images = await _context.Images.ToListAsync();
            ViewBag.Images = images; // Assign images to ViewBag because there is no image[] in model 

            _logger.LogInformation("Returning {PropertyCount} properties to the Index view.", properties.Count); // Log properties count

            return View(properties);
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            _logger.LogInformation("About action called."); 
            return View();
        }

        [AllowAnonymous]
        public IActionResult Contact()
        {
            _logger.LogInformation("Contact action called."); 
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Overview(int id)
        {
            _logger.LogInformation("Overview action called for PropertyId: {PropertyId}", id); // Log method access

            var property = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null)
            {
                _logger.LogWarning("Property not found for PropertyId: {PropertyId}", id); // Log warning if property is not found
                return NotFound();
            }

            return View(property);
        }
    }
}
