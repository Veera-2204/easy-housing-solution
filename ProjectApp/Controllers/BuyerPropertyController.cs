using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectApp.Data;
using ProjectApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace ProjectApp.Controllers
{
    /// <summary>
    /// The controller is used to display validated properties and filter them based on: CityName, PropertyType and Price
    /// </summary>
    public class BuyerPropertyController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly ILogger<BuyerPropertyController> _logger;

        public BuyerPropertyController(AppsDbContext context, ILogger<BuyerPropertyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Allow anonymous access to the index page
        [AllowAnonymous]
        public async Task<IActionResult> Index(string propertyOption, string cityName, string propertyType, string sortOrder, string stateName, int? id)
        {
            try
            {
                _logger.LogInformation("Index action called with parameters - PropertyOption: {PropertyOption}, CityName: {CityName}, PropertyType: {PropertyType}, SortOrder: {SortOrder}, StateName: {StateName}, ID: {ID}", 
                    propertyOption, cityName, propertyType, sortOrder, stateName, id);

                // Initialize ViewBag properties
                ViewBag.BuyerId = id;
                ViewBag.RequiresLogin = !User.Identity.IsAuthenticated;
                ViewBag.ErrorMessage = null;
                ViewBag.DatabaseError = false;

                // Validate buyer ID if user is authenticated
                if (User.Identity.IsAuthenticated && id.HasValue)
                {
                    try
                    {
                        var buyerExists = await _context.Buyers.AnyAsync(b => b.BuyerId == id.Value);
                        if (!buyerExists)
                        {
                            _logger.LogWarning("Invalid buyer ID provided: {BuyerId}", id.Value);
                            TempData["ErrorMessage"] = "Invalid buyer profile. Please log in again.";
                            return RedirectToAction("Login", "Account");
                        }
                    }
                    catch (Exception buyerValidationEx)
                    {
                        _logger.LogError(buyerValidationEx, "Error validating buyer ID: {BuyerId}", id);
                        // Continue without buyer validation rather than failing completely
                    }
                }

                // Initialize properties query with error handling
                IQueryable<Property> properties;
                try
                {
                    properties = _context.Properties
                        .Include(p => p.City)
                        .ThenInclude(c => c.State)
                        .AsQueryable();
                    
                    // Filter for active properties only
                    properties = properties.Where(p => p.IsActive);
                }
                catch (Exception queryEx)
                {
                    _logger.LogError(queryEx, "Error initializing properties query");
                    ViewBag.DatabaseError = true;
                    ViewBag.ErrorMessage = "Unable to load properties. Please try again later.";
                    return View(new List<Property>()); // Return empty list with error message
                }

                // Apply city filter with error handling
                if (!string.IsNullOrEmpty(cityName))
                {
                    try
                    {
                        _logger.LogInformation("Filtering properties by cityName: {CityName}", cityName);
                        
                        var cityIds = await _context.Cities
                            .Where(c => c.CityName == cityName)
                            .Select(c => c.CityId)
                            .ToListAsync();

                        _logger.LogInformation("Found {CityCount} cities with name: {CityName}", cityIds.Count, cityName);

                        if (cityIds.Any())
                        {
                            properties = properties.Where(p => p.CityId.HasValue && cityIds.Contains(p.CityId.Value));
                        }
                        else
                        {
                            _logger.LogWarning("No cities found for cityName: {CityName}", cityName);
                            properties = Enumerable.Empty<Property>().AsQueryable();
                        }
                    }
                    catch (Exception cityFilterEx)
                    {
                        _logger.LogError(cityFilterEx, "Error filtering by city: {CityName}", cityName);
                        ViewBag.ErrorMessage = "Error filtering by city. Showing all properties.";
                        // Continue without city filter rather than failing
                    }
                }

                // Apply property type filter with error handling
                if (!string.IsNullOrEmpty(propertyType))
                {
                    try
                    {
                        _logger.LogInformation("Filtering properties by PropertyType: {PropertyType}", propertyType);
                        properties = properties.Where(p => p.PropertyType == propertyType);
                    }
                    catch (Exception typeFilterEx)
                    {
                        _logger.LogError(typeFilterEx, "Error filtering by property type: {PropertyType}", propertyType);
                        ViewBag.ErrorMessage = "Error filtering by property type. Showing all properties.";
                        // Continue without type filter
                    }
                }

                // Apply sorting with error handling
                if (!string.IsNullOrEmpty(sortOrder))
                {
                    try
                    {
                        _logger.LogInformation("Sorting properties by PriceOrder: {SortOrder}", sortOrder);
                        properties = sortOrder == "Low to High"
                            ? properties.OrderBy(p => p.PriceRange)
                            : properties.OrderByDescending(p => p.PriceRange);
                    }
                    catch (Exception sortEx)
                    {
                        _logger.LogError(sortEx, "Error sorting properties by: {SortOrder}", sortOrder);
                        ViewBag.ErrorMessage = "Error sorting properties. Showing in default order.";
                        // Continue without sorting
                    }
                }

                // Filter out cart items for authenticated users with error handling
                if (User.Identity.IsAuthenticated && id.HasValue)
                {
                    try
                    {
                        var cartItems = await _context.Carts
                            .Where(c => c.BuyerId == id)
                            .Select(c => c.PropertyId)
                            .ToListAsync();

                        if (cartItems.Any())
                        {
                            _logger.LogInformation("Excluding {CartItemCount} properties already in cart for Buyer ID: {BuyerId}", cartItems.Count, id);
                            properties = properties.Where(p => !cartItems.Contains(p.PropertyId));
                        }
                    }
                    catch (Exception cartEx)
                    {
                        _logger.LogError(cartEx, "Error filtering cart items for buyer: {BuyerId}", id);
                        // Continue without cart filtering rather than failing
                    }
                }

                // Execute the query and get property IDs with error handling
                List<int> propertyIds;
                List<Property> finalProperties;
                try
                {
                    finalProperties = await properties.ToListAsync();
                    propertyIds = finalProperties.Select(p => p.PropertyId).ToList();
                    _logger.LogInformation("Retrieved {PropertyCount} properties after applying filters", finalProperties.Count);
                }
                catch (Exception queryExecutionEx)
                {
                    _logger.LogError(queryExecutionEx, "Error executing properties query");
                    ViewBag.DatabaseError = true;
                    ViewBag.ErrorMessage = "Database error occurred while loading properties. Please try again later.";
                    finalProperties = new List<Property>();
                    propertyIds = new List<int>();
                }

                // Load images with error handling
                List<Image> images = new List<Image>();
                try
                {
                    if (propertyIds.Any())
                    {
                        images = await _context.Images
                            .Where(img => propertyIds.Contains(img.PropertyId))
                            .ToListAsync();
                        _logger.LogInformation("Loaded {ImageCount} images for properties", images.Count);
                    }
                }
                catch (Exception imageEx)
                {
                    _logger.LogError(imageEx, "Error loading property images");
                    // Continue without images rather than failing
                    images = new List<Image>();
                }

                // Load dropdown data with error handling
                try
                {
                    ViewBag.Images = images;
                    
                    var cities = await _context.Cities.Select(p => p.CityName).Distinct().ToListAsync();
                    var states = await _context.States.Select(p => p.StateName).Distinct().ToListAsync();
                    var propertyTypes = await _context.Properties.Select(p => p.PropertyType).Distinct().ToListAsync();

                    ViewBag.cityList = new SelectList(cities);
                    ViewBag.stateList = new SelectList(states);
                    ViewBag.PropertyTypes = new SelectList(propertyTypes);
                    ViewBag.orderPrice = new SelectList(new List<string> { "Low to High", "High to Low" });

                    _logger.LogInformation("Dropdown data loaded successfully");
                }
                catch (Exception dropdownEx)
                {
                    _logger.LogError(dropdownEx, "Error loading dropdown data");
                    // Provide empty dropdowns rather than failing
                    ViewBag.Images = images;
                    ViewBag.cityList = new SelectList(new List<string>());
                    ViewBag.stateList = new SelectList(new List<string>());
                    ViewBag.PropertyTypes = new SelectList(new List<string>());
                    ViewBag.orderPrice = new SelectList(new List<string> { "Low to High", "High to Low" });
                }

                _logger.LogInformation("Index action completed successfully. Returning {PropertyCount} properties", finalProperties.Count);
                return View(finalProperties);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database connection error in BuyerProperty Index");
                ViewBag.DatabaseError = true;
                ViewBag.ErrorMessage = "Database connection error. Please try again later.";
                
                // Return minimal view data to prevent complete failure
                ViewBag.BuyerId = id;
                ViewBag.RequiresLogin = !User.Identity.IsAuthenticated;
                ViewBag.Images = new List<Image>();
                ViewBag.cityList = new SelectList(new List<string>());
                ViewBag.stateList = new SelectList(new List<string>());
                ViewBag.PropertyTypes = new SelectList(new List<string>());
                ViewBag.orderPrice = new SelectList(new List<string> { "Low to High", "High to Low" });
                
                return View(new List<Property>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in BuyerProperty Index with parameters - PropertyOption: {PropertyOption}, CityName: {CityName}, PropertyType: {PropertyType}, SortOrder: {SortOrder}, StateName: {StateName}, ID: {ID}", 
                    propertyOption, cityName, propertyType, sortOrder, stateName, id);
                
                ViewBag.ErrorMessage = "An unexpected error occurred while loading properties. Please try again later.";
                ViewBag.DatabaseError = true;
                
                // Return minimal view data to prevent complete failure
                ViewBag.BuyerId = id;
                ViewBag.RequiresLogin = !User.Identity.IsAuthenticated;
                ViewBag.Images = new List<Image>();
                ViewBag.cityList = new SelectList(new List<string>());
                ViewBag.stateList = new SelectList(new List<string>());
                ViewBag.PropertyTypes = new SelectList(new List<string>());
                ViewBag.orderPrice = new SelectList(new List<string> { "Low to High", "High to Low" });
                
                return View(new List<Property>());
            }
        }
    }
}