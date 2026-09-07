using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Data;
using ProjectApp.Models;
using ProjectApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Controllers
{
    /// <summary>
    /// The controller is for buyer for the functionalities of:
    /// For creating buyer with given details and calling the buyer dashboard
    /// </summary>
    [Authorize(Roles = "Buyer")]
    public class BuyerController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<BuyerController> _logger;

        public BuyerController(AppsDbContext context, UserManager<IdentityUser> userManager, ILogger<BuyerController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> CreateBuyerDetails()
        {
            try
            {
                _logger.LogInformation("CreateBuyerDetails (GET) accessed.");

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    _logger.LogInformation("User found with Email: {Email}.", user.Email);

                    var model = new BuyerViewModel
                    {
                        EmailId = user.Email,
                        DateOfBirth = DateTime.Now
                    };
                    return View(model);
                }

                _logger.LogWarning("No user found for the current session.");
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading CreateBuyerDetails page.");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return RedirectToAction("Login", "Account");
            }
        }

        // POST: Buyer/CreateBuyerDetails
        [HttpPost]
        public async Task<IActionResult> CreateBuyerDetails(BuyerViewModel model)
        {
            // Initialize ViewBag properties for error handling
            ViewBag.MinorDateOfBirth = false;
            ViewBag.MajorDateOfBirth = false;
            ViewBag.PhoneNumberValid = true;
            ViewBag.DatabaseError = false;

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateBuyerDetails. User: {Email}", model?.EmailId ?? "Unknown");
                    return View(model);
                }

                _logger.LogInformation("Processing CreateBuyerDetails for user: {Email}", model.EmailId);

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
                    ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old to register as a buyer.");
                    return View(model);
                }
                else if (age >= 100)
                {
                    _logger.LogWarning("Age validation failed: User appears to be {Age} years old", age);
                    ViewBag.MajorDateOfBirth = true;
                    ModelState.AddModelError("DateOfBirth", "Please enter a valid date of birth.");
                    return View(model);
                }

                // Validate phone number
                if (string.IsNullOrWhiteSpace(model.PhoneNo) || model.PhoneNo.Length != 10 || !model.PhoneNo.All(char.IsDigit))
                {
                    _logger.LogWarning("Phone number validation failed: {PhoneNo}", model.PhoneNo);
                    ViewBag.PhoneNumberValid = false;
                    ModelState.AddModelError("PhoneNo", "Phone number must be exactly 10 digits.");
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogError("No user found in session during buyer creation.");
                    ModelState.AddModelError(string.Empty, "Session expired. Please log in again.");
                    return View(model);
                }

                // Check if buyer already exists for this user
                var existingBuyer = await _context.Buyers.FirstOrDefaultAsync(b => b.UserId == user.Id);
                if (existingBuyer != null)
                {
                    _logger.LogWarning("Buyer already exists for user: {Email}", user.Email);
                    return RedirectToAction("BuyerDashboard", new { buyerId = existingBuyer.BuyerId });
                }

                // Create new buyer
                var buyer = new Buyer
                {
                    FirstName = model.FirstName?.Trim(),
                    LastName = model.LastName?.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    PhoneNo = model.PhoneNo.Trim(),
                    EmailId = user.Email,
                    UserId = user.Id
                };

                _context.Buyers.Add(buyer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Buyer created successfully with BuyerId: {BuyerId}", buyer.BuyerId);
                TempData["SuccessMessage"] = "Profile created successfully! Welcome to EasyHousing.";

                return RedirectToAction("BuyerDashboard", new { buyerId = buyer.BuyerId });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred during buyer creation for user: {Email}", model?.EmailId ?? "Unknown");
                ViewBag.DatabaseError = true;
                ModelState.AddModelError(string.Empty, "A database error occurred. Please try again later.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during buyer creation for user: {Email}", model?.EmailId ?? "Unknown");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        // GET: Buyer/BuyerDashboard
        public async Task<IActionResult> BuyerDashboard(int buyerId)
        {
            try
            {
                _logger.LogInformation("BuyerDashboard accessed for BuyerId: {BuyerId}", buyerId);

                if (buyerId <= 0)
                {
                    _logger.LogWarning("Invalid BuyerId provided: {BuyerId}", buyerId);
                    TempData["ErrorMessage"] = "Invalid buyer information.";
                    return RedirectToAction("CreateBuyerDetails");
                }

                var buyer = await _context.Buyers
                    .FirstOrDefaultAsync(b => b.BuyerId == buyerId);

                if (buyer == null)
                {
                    _logger.LogWarning("Buyer not found for BuyerId: {BuyerId}", buyerId);
                    TempData["ErrorMessage"] = "Buyer profile not found. Please complete your profile.";
                    return RedirectToAction("CreateBuyerDetails");
                }

                // Verify that the current user owns this buyer profile
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("No user found in session for BuyerDashboard access");
                    return RedirectToAction("Login", "Account");
                }

                if (buyer.UserId != currentUser.Id)
                {
                    _logger.LogWarning("Unauthorized access attempt to BuyerId: {BuyerId} by User: {UserId}", buyerId, currentUser.Id);
                    TempData["ErrorMessage"] = "Unauthorized access to buyer profile.";
                    return RedirectToAction("CreateBuyerDetails");
                }

                // Get cart items for statistics
                var cartItems = await _context.Carts
                    .Include(c => c.Property)
                        .ThenInclude(p => p.Images)
                    .Where(c => c.BuyerId == buyerId)
                    .ToListAsync();

                _logger.LogInformation("BuyerDashboard loaded successfully for BuyerId: {BuyerId} with {CartCount} cart items", buyerId, cartItems.Count);

                ViewBag.BuyerId = buyer.BuyerId;
                ViewBag.CartItems = cartItems;

                return View(buyer);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while loading BuyerDashboard for BuyerId: {BuyerId}", buyerId);
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return RedirectToAction("CreateBuyerDetails");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading BuyerDashboard for BuyerId: {BuyerId}", buyerId);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading your dashboard.";
                return RedirectToAction("CreateBuyerDetails");
            }
        }

        // GET: Buyer/Profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                _logger.LogInformation("Buyer Profile (GET) accessed.");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("No user found for the current session.");
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var buyer = await _context.Buyers
                    .FirstOrDefaultAsync(b => b.UserId == user.Id);

                if (buyer == null)
                {
                    _logger.LogWarning("Buyer profile not found for user: {Email}", user.Email);
                    TempData["ErrorMessage"] = "Profile not found. Please complete your profile setup.";
                    return RedirectToAction("CreateBuyerDetails");
                }

                // Get buyer statistics
                var cartCount = await _context.Carts
                    .CountAsync(c => c.BuyerId == buyer.BuyerId);

                var viewModel = new BuyerViewModel
                {
                    FirstName = buyer.FirstName,
                    LastName = buyer.LastName,
                    DateOfBirth = buyer.DateOfBirth,
                    PhoneNo = buyer.PhoneNo,
                    EmailId = buyer.EmailId
                };

                ViewBag.BuyerId = buyer.BuyerId;
                ViewBag.CartCount = cartCount;
                ViewBag.AccountCreated = user.EmailConfirmed;

                _logger.LogInformation("Profile loaded successfully for BuyerId: {BuyerId}", buyer.BuyerId);
                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while loading buyer profile for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return RedirectToAction("CreateBuyerDetails");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading buyer profile for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading your profile.";
                return RedirectToAction("CreateBuyerDetails");
            }
        }

        // POST: Buyer/UpdateProfile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(BuyerViewModel model)
        {
            try
            {
                _logger.LogInformation("UpdateProfile (POST) accessed.");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("No user found for the current session during profile update.");
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var buyer = await _context.Buyers
                    .FirstOrDefaultAsync(b => b.UserId == user.Id);

                if (buyer == null)
                {
                    _logger.LogWarning("Buyer profile not found for user: {Email} during profile update", user.Email);
                    TempData["ErrorMessage"] = "Profile not found. Please complete your profile setup.";
                    return RedirectToAction("CreateBuyerDetails");
                }

                // Server-side validation
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for profile update. BuyerId: {BuyerId}", buyer.BuyerId);
                    
                    // Re-populate ViewBag for the view
                    ViewBag.BuyerId = buyer.BuyerId;
                    var cartCount = await _context.Carts.CountAsync(c => c.BuyerId == buyer.BuyerId);
                    ViewBag.CartCount = cartCount;
                    ViewBag.AccountCreated = user.EmailConfirmed;
                    
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

                // Check for validation errors after custom validation
                if (!ModelState.IsValid)
                {
                    ViewBag.BuyerId = buyer.BuyerId;
                    var cartCount = await _context.Carts.CountAsync(c => c.BuyerId == buyer.BuyerId);
                    ViewBag.CartCount = cartCount;
                    ViewBag.AccountCreated = user.EmailConfirmed;
                    
                    return View("Profile", model);
                }

                // Update buyer information
                buyer.FirstName = model.FirstName?.Trim();
                buyer.LastName = model.LastName?.Trim();
                buyer.DateOfBirth = model.DateOfBirth;
                buyer.PhoneNo = model.PhoneNo.Trim();

                _context.Buyers.Update(buyer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated successfully for BuyerId: {BuyerId}", buyer.BuyerId);
                TempData["SuccessMessage"] = "Profile updated successfully!";
                
                return RedirectToAction("Profile");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred during profile update for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "A database error occurred while updating your profile. Please try again later.";
                
                // Try to get buyer info for ViewBag
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var buyer = await _context.Buyers.FirstOrDefaultAsync(b => b.UserId == user.Id);
                        if (buyer != null)
                        {
                            ViewBag.BuyerId = buyer.BuyerId;
                            var cartCount = await _context.Carts.CountAsync(c => c.BuyerId == buyer.BuyerId);
                            ViewBag.CartCount = cartCount;
                            ViewBag.AccountCreated = user.EmailConfirmed;
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
                _logger.LogError(ex, "Unexpected error occurred during profile update for user: {Email}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "An unexpected error occurred while updating your profile. Please try again.";
                
                // Try to get buyer info for ViewBag
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var buyer = await _context.Buyers.FirstOrDefaultAsync(b => b.UserId == user.Id);
                        if (buyer != null)
                        {
                            ViewBag.BuyerId = buyer.BuyerId;
                            var cartCount = await _context.Carts.CountAsync(c => c.BuyerId == buyer.BuyerId);
                            ViewBag.CartCount = cartCount;
                            ViewBag.AccountCreated = user.EmailConfirmed;
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
    }
}