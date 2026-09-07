using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Data;
using ProjectApp.Models;
using ProjectApp.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectApp.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<SellerController> _logger;

        public SellerController(AppsDbContext context, UserManager<IdentityUser> userManager, ILogger<SellerController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Seller/CreateSellerDetails
        public async Task<IActionResult> CreateSellerDetails()
        {
            try
            {
                _logger.LogInformation("CreateSellerDetails (GET) accessed.");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("No user found for the current session.");
                    TempData["ErrorMessage"] = "Session error. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Load states for dropdown with error handling
                List<SelectListItem> stateList;
                try
                {
                    stateList = await _context.States.Select(s => new SelectListItem
                    {
                        Value = s.StateName,
                        Text = s.StateName
                    }).ToListAsync();
                }
                catch (Exception stateEx)
                {
                    _logger.LogError(stateEx, "Error loading states for dropdown");
                    stateList = new List<SelectListItem>();
                    ViewBag.StateLoadError = true;
                }

                ViewBag.stateList = stateList;

                var model = new SellerViewModel
                {
                    UserName = user.UserName,
                    EmailId = user.Email,
                    DateOfBirth = DateTime.Now
                };

                _logger.LogInformation("CreateSellerDetails page loaded for User: {UserName}", user.UserName);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading CreateSellerDetails page.");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return RedirectToAction("Login", "Account");
            }
        }

        // POST: Seller/CreateSellerDetails
        [HttpPost]
        public async Task<IActionResult> CreateSellerDetails(SellerViewModel model)
        {
            // Initialize ViewBag properties for error handling
            ViewBag.MinorDateOfBirth = false;
            ViewBag.MajorDateOfBirth = false;
            ViewBag.PhoneNumberValid = true;
            ViewBag.DatabaseError = false;
            ViewBag.StateNotFound = false;

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateSellerDetails. User: {UserName}", model?.UserName ?? "Unknown");
                    await LoadStatesForDropdown();
                    return View(model);
                }

                _logger.LogInformation("Processing Seller creation for User: {UserName}", model.UserName);

                // Validate state exists
                var state = await _context.States
                    .FirstOrDefaultAsync(s => s.StateName == model.StateName);

                if (state == null)
                {
                    _logger.LogWarning("State not found: {StateName}", model.StateName);
                    ViewBag.StateNotFound = true;
                    ModelState.AddModelError("StateName", "Selected state does not exist.");
                    await LoadStatesForDropdown();
                    return View(model);
                }

                // Handle city creation/retrieval with error handling
                City city;
                try
                {
                    city = await _context.Cities
                        .FirstOrDefaultAsync(c => c.CityName == model.CityName && c.StateId == state.StateId);

                    if (city == null)
                    {
                        city = new City { CityName = model.CityName, StateId = state.StateId };
                        _context.Cities.Add(city);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Created new city: {CityName} in StateId: {StateId}", model.CityName, state.StateId);
                    }
                }
                catch (Exception cityEx)
                {
                    _logger.LogError(cityEx, "Error creating/retrieving city: {CityName}", model.CityName);
                    ModelState.AddModelError(string.Empty, "Error processing city information. Please try again.");
                    await LoadStatesForDropdown();
                    return View(model);
                }

                // Validate age
                int age = DateTime.Now.Year - model.DateOfBirth.Year;
                if (model.DateOfBirth > DateTime.Now.AddYears(-age))
                {
                    age--; // Adjust if birthday hasn't occurred this year
                }

                if (age < 18)
                {
                    _logger.LogWarning("Age validation failed: User is {Age} years old", age);
                    ViewBag.MinorDateOfBirth = true;
                    ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old to register as a seller.");
                    await LoadStatesForDropdown();
                    return View(model);
                }
                else if (age >= 100)
                {
                    _logger.LogWarning("Age validation failed: User appears to be {Age} years old", age);
                    ViewBag.MajorDateOfBirth = true;
                    ModelState.AddModelError("DateOfBirth", "Please enter a valid date of birth.");
                    await LoadStatesForDropdown();
                    return View(model);
                }

                // Validate phone number
                if (string.IsNullOrWhiteSpace(model.PhoneNo) || model.PhoneNo.Length != 10 || !model.PhoneNo.All(char.IsDigit))
                {
                    _logger.LogWarning("Phone number validation failed: {PhoneNo}", model.PhoneNo);
                    ViewBag.PhoneNumberValid = false;
                    ModelState.AddModelError("PhoneNo", "Phone number must be exactly 10 digits.");
                    await LoadStatesForDropdown();
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogError("No user found in session during seller creation.");
                    ModelState.AddModelError(string.Empty, "Session expired. Please log in again.");
                    await LoadStatesForDropdown();
                    return View(model);
                }

                // Check if seller already exists for this user
                var existingSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (existingSeller != null)
                {
                    _logger.LogWarning("Seller already exists for user: {Email}", user.Email);
                    return RedirectToAction(nameof(SellerDashboard), new { sellerId = existingSeller.SellerId });
                }

                // Create new seller
                var seller = new Seller
                {
                    UserName = user.UserName,
                    FirstName = model.FirstName?.Trim(),
                    LastName = model.LastName?.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    PhoneNo = model.PhoneNo.Trim(),
                    Address = model.Address?.Trim(),
                    EmailId = user.Email,
                    StateId = state.StateId,
                    CityId = city.CityId,
                    UserId = user.Id
                };

                _context.Sellers.Add(seller);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seller created with SellerId: {SellerId}", seller.SellerId);
                TempData["SuccessMessage"] = "Profile created successfully! Welcome to EasyHousing.";

                return RedirectToAction(nameof(SellerDashboard), new { sellerId = seller.SellerId });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while creating seller for user: {UserName}", model?.UserName ?? "Unknown");
                ViewBag.DatabaseError = true;
                ModelState.AddModelError(string.Empty, "A database error occurred. Please try again later.");
                await LoadStatesForDropdown();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating seller for user: {UserName}", model?.UserName ?? "Unknown");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                await LoadStatesForDropdown();
                return View(model);
            }
        }

        // GET: Seller/SellerDashboard
        public async Task<IActionResult> SellerDashboard(int sellerId)
        {
            try
            {
                _logger.LogInformation("SellerDashboard accessed for SellerId: {SellerId}", sellerId);

                if (sellerId <= 0)
                {
                    _logger.LogWarning("Invalid SellerId provided: {SellerId}", sellerId);
                    TempData["ErrorMessage"] = "Invalid seller information.";
                    return RedirectToAction("CreateSellerDetails");
                }

                var seller = await _context.Sellers
                    .Include(s => s.State)
                    .Include(s => s.City)
                    .Include(s => s.Properties)
                        .ThenInclude(p => p.City)
                    .Include(s => s.Properties)
                        .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync(s => s.SellerId == sellerId);

                if (seller == null)
                {
                    _logger.LogWarning("Seller not found with SellerId: {SellerId}", sellerId);
                    TempData["ErrorMessage"] = "Seller profile not found. Please complete your profile.";
                    return RedirectToAction("CreateSellerDetails");
                }

                // Verify that the current user owns this seller profile
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("No user found in session for SellerDashboard access");
                    return RedirectToAction("Login", "Account");
                }

                if (seller.UserId != currentUser.Id)
                {
                    _logger.LogWarning("Unauthorized access attempt to SellerId: {SellerId} by User: {UserId}", sellerId, currentUser.Id);
                    TempData["ErrorMessage"] = "Unauthorized access to seller profile.";
                    return RedirectToAction("CreateSellerDetails");
                }

                _logger.LogInformation("SellerDashboard loaded for SellerId: {SellerId}", sellerId);
                ViewBag.SellerId = seller.SellerId;

                // Get filtered properties based on status if filter is provided
                string filter = HttpContext.Request.Query["filter"].ToString();
                var properties = seller.Properties;

                if (!string.IsNullOrEmpty(filter))
                {
                    switch (filter.ToLower())
                    {
                        case "active":
                            properties = properties.Where(p => p.IsActive).ToList();
                            break;
                        case "inactive":
                            properties = properties.Where(p => !p.IsActive).ToList();
                            break;
                        // "all" case will use unfiltered properties
                    }
                }

                seller.Properties = properties.ToList();
                return View(seller);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while loading SellerDashboard for SellerId: {SellerId}", sellerId);
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return RedirectToAction("CreateSellerDetails");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading SellerDashboard for SellerId: {SellerId}", sellerId);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading your dashboard.";
                return RedirectToAction("CreateSellerDetails");
            }
        }

        // GET: Seller/PropertyDetails
        public async Task<IActionResult> PropertyDetails(int propertyId)
        {
            try
            {
                _logger.LogInformation("PropertyDetails accessed for PropertyId: {PropertyId}", propertyId);

                if (propertyId <= 0)
                {
                    _logger.LogWarning("Invalid PropertyId provided: {PropertyId}", propertyId);
                    TempData["ErrorMessage"] = "Invalid property information.";
                    return RedirectToAction("SellerDashboard");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not authenticated for PropertyDetails access");
                    return RedirectToAction("Login", "Account");
                }

                var property = await _context.Properties
                    .Include(p => p.Seller)
                    .Include(p => p.Images)
                    .Include(p => p.City)
                        .ThenInclude(c => c.State)
                    .FirstOrDefaultAsync(p => p.PropertyId == propertyId);

                if (property == null)
                {
                    _logger.LogWarning("Property not found for PropertyId: {PropertyId}", propertyId);
                    TempData["ErrorMessage"] = "Property not found.";
                    return RedirectToAction("SellerDashboard");
                }

                // Ensure the property belongs to the current seller
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (seller == null)
                {
                    _logger.LogWarning("Seller profile not found for user: {UserId}", user.Id);
                    TempData["ErrorMessage"] = "Seller profile not found. Please complete your profile.";
                    return RedirectToAction("CreateSellerDetails");
                }

                if (property.SellerId != seller.SellerId)
                {
                    _logger.LogWarning("Unauthorized access attempt to PropertyId: {PropertyId} by User: {UserId}", propertyId, user.Id);
                    TempData["ErrorMessage"] = "Unauthorized access to property details.";
                    return RedirectToAction("SellerDashboard", new { sellerId = seller.SellerId });
                }

                var viewModel = new PropertyDetailsViewModel
                {
                    PropertyId = property.PropertyId,
                    PropertyName = property.PropertyName,
                    PropertyType = property.PropertyType,
                    PropertyOption = property.PropertyOption,
                    InitialDeposit = property.InitialDeposit,
                    Description = property.Description,
                    Address = property.Address,
                    Price = property.PriceRange,
                    Landmark = property.Landmark,
                    IsActive = property.IsActive,
                    CityName = property.City?.CityName,
                    StateName = property.City?.State?.StateName,
                    SellerName = property.Seller?.FirstName + " " + property.Seller?.LastName,
                    SellerContactInfo = property.Seller?.PhoneNo,
                    SellerEmailId = property.Seller?.EmailId,
                    Images = property.Images.ToList(),
                };

                ViewBag.SellerId = seller.SellerId;
                _logger.LogInformation("PropertyDetails loaded for PropertyId: {PropertyId} by SellerId: {SellerId}", propertyId, seller.SellerId);

                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while loading PropertyDetails for PropertyId: {PropertyId}", propertyId);
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return RedirectToAction("SellerDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading PropertyDetails for PropertyId: {PropertyId}", propertyId);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading property details.";
                return RedirectToAction("SellerDashboard");
            }
        }

        // GET: Seller/Profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                _logger.LogInformation("Seller Profile (GET) accessed.");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("No user found for the current session.");
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var seller = await _context.Sellers
                    .Include(s => s.State)
                    .Include(s => s.City)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (seller == null)
                {
                    _logger.LogWarning("Seller profile not found for user: {Email}", user.Email);
                    TempData["ErrorMessage"] = "Profile not found. Please complete your profile setup.";
                    return RedirectToAction("CreateSellerDetails");
                }

                // Get seller statistics
                var activePropertiesCount = await _context.Properties
                    .CountAsync(p => p.SellerId == seller.SellerId && p.IsActive);
                
                var totalPropertiesCount = await _context.Properties
                    .CountAsync(p => p.SellerId == seller.SellerId);

                var viewModel = new SellerViewModel
                {
                    UserName = seller.UserName,
                    FirstName = seller.FirstName,
                    LastName = seller.LastName,
                    DateOfBirth = seller.DateOfBirth,
                    PhoneNo = seller.PhoneNo,
                    EmailId = seller.EmailId,
                    Address = seller.Address,
                    StateName = seller.State?.StateName,
                    CityName = seller.City?.CityName
                };

                ViewBag.SellerId = seller.SellerId;
                ViewBag.ActivePropertiesCount = activePropertiesCount;
                ViewBag.TotalPropertiesCount = totalPropertiesCount;
                ViewBag.AccountCreated = user.EmailConfirmed;

                _logger.LogInformation("Profile loaded for SellerId: {SellerId}", seller.SellerId);
                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while loading seller profile for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return RedirectToAction("CreateSellerDetails");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading seller profile for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading your profile.";
                return RedirectToAction("CreateSellerDetails");
            }
        }

        // POST: Seller/UpdateProfile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(SellerViewModel model)
        {
            try
            {
                _logger.LogInformation("Seller UpdateProfile (POST) accessed.");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("No user found for the current session during profile update.");
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var seller = await _context.Sellers
                    .Include(s => s.State)
                    .Include(s => s.City)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (seller == null)
                {
                    _logger.LogWarning("Seller profile not found for user: {Email} during profile update", user.Email);
                    TempData["ErrorMessage"] = "Profile not found. Please complete your profile setup.";
                    return RedirectToAction("CreateSellerDetails");
                }

                // Server-side validation
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for seller profile update. SellerId: {SellerId}", seller.SellerId);
                    
                    // Re-populate ViewBag for the view
                    ViewBag.SellerId = seller.SellerId;
                    ViewBag.stateList = await GetStatesForDropdown();
                    
                    return View("Profile", model);
                }

                // Validate age
                int age = DateTime.Now.Year - model.DateOfBirth.Year;
                if (model.DateOfBirth > DateTime.Now.AddYears(-age))
                {
                    age--; // Adjust if birthday hasn't occurred this year
                }

                if (age < 18)
                {
                    _logger.LogWarning("Age validation failed during profile update: User is {Age} years old", age);
                    ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old.");
                }
                else if (age >= 100)
                {
                    _logger.LogWarning("Age validation failed during profile update: User appears to be {Age} years old", age);
                    ModelState.AddModelError("DateOfBirth", "Please enter a valid date of birth.");
                }

                // Validate phone number
                if (string.IsNullOrWhiteSpace(model.PhoneNo) || model.PhoneNo.Length != 10 || !model.PhoneNo.All(char.IsDigit))
                {
                    _logger.LogWarning("Phone number validation failed during profile update: {PhoneNo}", model.PhoneNo);
                    ModelState.AddModelError("PhoneNo", "Phone number must be exactly 10 digits.");
                }

                // Handle State and City updates
                if (!string.IsNullOrEmpty(model.StateName) && !string.IsNullOrEmpty(model.CityName))
                {
                    var state = await _context.States
                        .FirstOrDefaultAsync(s => s.StateName == model.StateName);

                    if (state == null)
                    {
                        ModelState.AddModelError("StateName", "Selected state does not exist.");
                    }
                    else
                    {
                        var city = await _context.Cities
                            .FirstOrDefaultAsync(c => c.CityName == model.CityName && c.StateId == state.StateId);

                        if (city == null)
                        {
                            // Create new city if it doesn't exist
                            city = new City { CityName = model.CityName, StateId = state.StateId };
                            _context.Cities.Add(city);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Created new city: {CityName} in StateId: {StateId}", model.CityName, state.StateId);
                        }

                        seller.StateId = state.StateId;
                        seller.CityId = city.CityId;
                    }
                }

                // Check for validation errors after custom validation
                if (!ModelState.IsValid)
                {
                    ViewBag.SellerId = seller.SellerId;
                    ViewBag.stateList = await GetStatesForDropdown();
                    
                    return View("Profile", model);
                }

                // Update seller information
                seller.FirstName = model.FirstName?.Trim();
                seller.LastName = model.LastName?.Trim();
                seller.DateOfBirth = model.DateOfBirth;
                seller.PhoneNo = model.PhoneNo.Trim();
                seller.Address = model.Address?.Trim();

                _context.Sellers.Update(seller);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated successfully for SellerId: {SellerId}", seller.SellerId);
                TempData["SuccessMessage"] = "Profile updated successfully!";
                
                return RedirectToAction("Profile");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred during seller profile update for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "A database error occurred while updating your profile. Please try again later.";
                
                // Try to get seller info for ViewBag
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                        if (seller != null)
                        {
                            ViewBag.SellerId = seller.SellerId;
                            ViewBag.stateList = await GetStatesForDropdown();
                        }
                    }
                }
                catch
                {
                    // If we can't get ViewBag info, redirect to safe location
                    return RedirectToAction("Profile");
                }
                
                return View("Profile", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during seller profile update for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "An unexpected error occurred while updating your profile. Please try again.";
                
                // Try to get seller info for ViewBag
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                        if (seller != null)
                        {
                            ViewBag.SellerId = seller.SellerId;
                            ViewBag.stateList = await GetStatesForDropdown();
                        }
                    }
                }
                catch
                {
                    // If we can't get ViewBag info, redirect to safe location
                    return RedirectToAction("Profile");
                }
                
                return View("Profile", model);
            }
        }

        // Helper method to load states for dropdown
        private async Task LoadStatesForDropdown()
        {
            try
            {
                ViewBag.stateList = await _context.States.Select(s => new SelectListItem
                {
                    Value = s.StateName,
                    Text = s.StateName
                }).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading states for dropdown");
                ViewBag.stateList = new List<SelectListItem>();
                ViewBag.StateLoadError = true;
            }
        }

        // Helper method to get states for dropdown
        private async Task<List<SelectListItem>> GetStatesForDropdown()
        {
            try
            {
                return await _context.States.Select(s => new SelectListItem
                {
                    Value = s.StateName,
                    Text = s.StateName
                }).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting states for dropdown");
                return new List<SelectListItem>();
            }
        }
    }
}