using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProjectApp.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectApp.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjectApp.Data;

namespace ProjectApp.Controllers
{
    /// <summary>
    /// Controller for the Admin: Viewing the Dashboard, Filtering by seller, region, sort order and viewing a property
    /// and validating/in-validating a property
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger; // Add logger

        public AdminController(AppsDbContext context, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger) // Inject logger
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger; // Assign logger
        }

        public IActionResult AdminDashboard()
        {
            _logger.LogInformation("AdminDashboard accessed."); // Log information
            return View();
        }

        // View Properties by Region
        [HttpGet]
        public IActionResult ViewByRegion(/*string? stateName,*/ string? cityName, string? sortOrder)
        {
            _logger.LogInformation("ViewByRegion accessed with cityName: {cityName}, sortOrder: {sortOrder}.", cityName, sortOrder);

            var properties = _context.Properties.AsQueryable();

            // Filter by city name if provided
            if (!string.IsNullOrEmpty(cityName) /*&& string.IsNullOrEmpty(stateName)*/)
            {
                _logger.LogInformation("Filtering properties by city: {cityName}.", cityName);
                int cityId = _context.Cities
                                .Where(c => c.CityName == cityName)
                                .Select(c => c.CityId)
                                .FirstOrDefault();
                if (cityId != 0)
                {
                    properties = properties.Where(p => p.CityId == cityId);
                }
            }

/*            // Filter by state name if provided
            if (!string.IsNullOrEmpty(stateName) && string.IsNullOrEmpty(cityName))
            {
                _logger.LogInformation("Filtering properties by state: {stateName}.", stateName);
                int stateId = _context.States
                                .Where(c => c.StateName == stateName)
                                .Select(c => c.StateId)
                                .FirstOrDefault();

                if (stateId != 0)
                {
                    List<int> cityIds = _context.Cities
                                            .Where(c => c.StateId == stateId)
                                            .Select(c => c.CityId)
                                            .ToList();
                    List<int> sellerIds = _context.Sellers
                                            .Where(s => cityIds.Contains(s.CityId))
                                            .Select(s => s.SellerId)
                                            .ToList();
                    properties = properties.Where(p => p.IsActive && sellerIds.Contains(p.SellerId));
                }
            }*/

/*            // Filter by city name and state name if both are provided
            if (!string.IsNullOrEmpty(stateName) && !string.IsNullOrEmpty(cityName))
            {
                _logger.LogInformation("Filtering properties by city: {cityName} and state: {stateName}.", cityName, stateName);
                int cityId = _context.Cities
                                .Where(c => c.CityName == cityName)
                                .Select(c => c.CityId)
                                .FirstOrDefault();
                if (cityId != 0)
                {
                    properties = properties.Where(p => p.CityId == cityId);
                }
            }*/

            // Sort by price if sortOrder is provided
            if (!string.IsNullOrEmpty(sortOrder))
            {
                _logger.LogInformation("Sorting properties by price: {sortOrder}.", sortOrder);
                if (sortOrder == "Low to High")
                {
                    properties = properties.OrderBy(p => p.PriceRange);
                }
                else
                {
                    properties = properties.OrderByDescending(p => p.PriceRange);
                }
            }

            // Set up ViewBag for dropdowns
            ViewBag.cityList = new SelectList(_context.Cities, "CityName", "CityName");
            ViewBag.orderPrice = new SelectList(new List<string> { "Low to High", "High to Low" });

            return View(properties.ToList());
        }

        public IActionResult Details(int propertyId)
        {
            _logger.LogInformation("Details accessed for propertyId: {propertyId}.", propertyId);
            var property = _context.Properties
                .Include(p => p.Seller)
                .Include(p => p.Images)
                .FirstOrDefault(p => p.PropertyId == propertyId);

            if (property == null)
            {
                _logger.LogWarning("Property not found for propertyId: {propertyId}.", propertyId);
                return NotFound();
            }

            var viewModel = new PropertyDetailsViewModel
            {
                PropertyId = property.PropertyId,
                PropertyName = property.PropertyName,
                PropertyOption = property.PropertyOption,
                InitialDeposit = property.InitialDeposit,
                Description = property.Description,
                Address = property.Address,
                Price = property.PriceRange,
                SellerName = property.Seller?.FirstName,
                SellerContactInfo = property.Seller?.PhoneNo,
                Images = property.Images.ToList(),
            };

            return View("Details", viewModel);
        }

        public IActionResult OwnerList()
        {
            _logger.LogInformation("SellerList accessed by Admin.");
            var sellers = _context.Sellers
                .Include(s => s.State)
                .Include(s => s.City)
                .ToList();
            return View(sellers);
        }

        public IActionResult PropertiesBySeller(int sellerId)
        {
            _logger.LogInformation("PropertiesBySeller accessed for sellerId: {sellerId}.", sellerId);
            var seller = _context.Sellers
                .Include(s => s.Properties)
                .FirstOrDefault(s => s.SellerId == sellerId);

            if (seller == null)
            {
                _logger.LogWarning("Seller not found for sellerId: {sellerId}.", sellerId);
                return NotFound();
            }

            var viewModel = new SellerPropertiesViewModel
            {
                Seller = seller,
                Properties = (List<Property>)seller.Properties
            };

            return View(viewModel);
        }

        public IActionResult ActivePropertiesBySeller(int sellerId)
        {
            _logger.LogInformation("ActivePropertiesBySeller accessed for sellerId: {sellerId}.", sellerId);
            var seller = _context.Sellers
                .Include(s => s.Properties)
                .FirstOrDefault(s => s.SellerId == sellerId);

            if (seller == null)
            {
                _logger.LogWarning("Seller not found for sellerId: {sellerId}.", sellerId);
                return NotFound();
            }

            var viewModel = new SellerPropertiesViewModel
            {
                Seller = seller,
                Properties = seller.Properties.Where(p => p.IsActive).ToList()
            };

            return View("PropertiesBySeller", viewModel);
        }

        public IActionResult NonActivePropertiesBySeller(int sellerId)
        {
            _logger.LogInformation("NonActivePropertiesBySeller accessed for sellerId: {sellerId}.", sellerId);
            var seller = _context.Sellers
                .Include(s => s.Properties)
                .FirstOrDefault(s => s.SellerId == sellerId);

            if (seller == null)
            {
                _logger.LogWarning("Seller not found for sellerId: {sellerId}.", sellerId);
                return NotFound();
            }

            var viewModel = new SellerPropertiesViewModel
            {
                Seller = seller,
                Properties = seller.Properties.Where(p => !p.IsActive).ToList()
            };

            return View("PropertiesBySeller", viewModel);
        }

        [HttpGet]
        public IActionResult ViewProperties()
        {
            _logger.LogInformation("ViewProperties accessed.");
            var properties = _context.Properties.ToList();
            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> DeactivateProperty(int id)
        {
            _logger.LogInformation("DeactivateProperty accessed for propertyId: {id}.", id);
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                _logger.LogWarning("Property not found for propertyId: {id}.", id);
                return NotFound();
            }

            property.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Property deactivated for propertyId: {id}.", id);
            return RedirectToAction("ViewProperties");
        }

        [HttpGet]
        public async Task<IActionResult> VerifyProperty(int id)
        {
            _logger.LogInformation("VerifyProperty accessed for propertyId: {id}.", id);
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                _logger.LogWarning("Property not found for propertyId: {id}.", id);
                return NotFound();
            }

            property.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Property verified for propertyId: {id}.", id);
            return RedirectToAction("ViewProperties");
        }

    }
}
