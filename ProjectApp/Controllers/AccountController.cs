using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectApp.ViewModels;
using ProjectApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Data;

namespace ProjectApp.Controllers
{
    /// <summary>
    /// The class defines methods for Register and Login for different types of users
    /// </summary>
    public class AccountController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppsDbContext context, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // Register action
        [HttpGet]
        public IActionResult Register()
        {
            try
            {
                _logger.LogInformation("Navigated to Register page.");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading Register page.");
                TempData["ErrorMessage"] = "An error occurred while loading the registration page. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Initialize ViewBag properties
            ViewBag.UserAlreadyExists = false;
            ViewBag.UserDetailsCorrect = true;
            ViewBag.UserCreationFailed = false;
            ViewBag.RoleAssignmentFailed = false;
            ViewBag.DatabaseError = false;

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for registration attempt. UserName: {UserName}", model?.UserName ?? "Unknown");
                    return View(model);
                }

                _logger.LogInformation("Registration attempt for user: {UserName}", model.UserName);

                // Check if user already exists before attempting creation
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed - user already exists: {UserName}", model.UserName);
                    ViewBag.UserAlreadyExists = true;
                    ModelState.AddModelError(string.Empty, "A user with this username already exists.");
                    return View(model);
                }

                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    _logger.LogWarning("Registration failed - email already exists: {Email}", model.Email);
                    ViewBag.UserAlreadyExists = true;
                    ModelState.AddModelError(string.Empty, "A user with this email address already exists.");
                    return View(model);
                }

                // Create new user
                var user = new AppsUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    UserType = model.UserType,
                    EmailConfirmed = false // Set to false initially
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created successfully: {UserName}", user.UserName);

                    try
                    {
                        // Create the role if it doesn't exist
                        if (!await _roleManager.RoleExistsAsync(model.UserType))
                        {
                            var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole(model.UserType));
                            if (!roleCreationResult.Succeeded)
                            {
                                _logger.LogError("Failed to create role: {Role}. Errors: {Errors}", 
                                    model.UserType, 
                                    string.Join(", ", roleCreationResult.Errors.Select(e => e.Description)));
                                
                                ViewBag.RoleAssignmentFailed = true;
                                ModelState.AddModelError(string.Empty, "Failed to create user role. Please contact support.");
                                
                                // Cleanup: Delete the created user since role creation failed
                                await _userManager.DeleteAsync(user);
                                return View(model);
                            }
                            _logger.LogInformation("Role created successfully: {Role}", model.UserType);
                        }

                        // Assign role to the user
                        var roleResult = await _userManager.AddToRoleAsync(user, model.UserType);

                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("User assigned to role successfully: {UserName} -> {Role}", user.UserName, model.UserType);

                            // Automatically sign in the user after registration
                            await _signInManager.SignInAsync(user, isPersistent: false);

                            // Prepare login model
                            var loginModel = new LoginViewModel
                            {
                                UserName = user.UserName,
                                Password = model.Password
                            };

                            // Call the Login action method to handle redirection based on role
                            return await Login(loginModel);
                        }
                        else
                        {
                            _logger.LogError("Failed to assign role to user: {UserName}. Errors: {Errors}", 
                                user.UserName, 
                                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            
                            ViewBag.RoleAssignmentFailed = true;
                            
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, $"Role assignment failed: {error.Description}");
                            }

                            // Cleanup: Delete the created user since role assignment failed
                            await _userManager.DeleteAsync(user);
                            return View(model);
                        }
                    }
                    catch (Exception roleEx)
                    {
                        _logger.LogError(roleEx, "Exception occurred during role creation/assignment for user: {UserName}", user.UserName);
                        ViewBag.RoleAssignmentFailed = true;
                        ModelState.AddModelError(string.Empty, "An error occurred while setting up your account. Please try again.");
                        
                        // Cleanup: Delete the created user
                        try
                        {
                            await _userManager.DeleteAsync(user);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogError(cleanupEx, "Failed to cleanup user after role assignment failure: {UserName}", user.UserName);
                        }
                        
                        return View(model);
                    }
                }
                else
                {
                    _logger.LogWarning("User creation failed for: {UserName}. Errors: {Errors}", 
                        model.UserName, 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    
                    // Check for specific error types
                    bool userExistsError = result.Errors.Any(e => e.Code.Contains("DuplicateUserName") || e.Code.Contains("DuplicateEmail"));
                    
                    if (userExistsError)
                    {
                        ViewBag.UserAlreadyExists = true;
                        ModelState.AddModelError(string.Empty, "User with this username or email already exists!");
                    }
                    else
                    {
                        ViewBag.UserDetailsCorrect = false;
                        ViewBag.UserCreationFailed = true;
                        
                        foreach (var error in result.Errors)
                        {
                            if (error.Code.Contains("Password"))
                            {
                                ModelState.AddModelError(nameof(model.Password), error.Description);
                            }
                            else if (error.Code.Contains("Email"))
                            {
                                ModelState.AddModelError(nameof(model.Email), error.Description);
                            }
                            else if (error.Code.Contains("UserName"))
                            {
                                ModelState.AddModelError(nameof(model.UserName), error.Description);
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }
                    
                    return View(model);
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred during registration for user: {UserName}", model?.UserName ?? "Unknown");
                ViewBag.DatabaseError = true;
                ModelState.AddModelError(string.Empty, "A database error occurred. Please try again later or contact support if the problem persists.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during registration for user: {UserName}", model?.UserName ?? "Unknown");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration. Please try again later.");
                return View(model);
            }
        }

        // Login action
        [HttpGet]
        public IActionResult Login()
        {
            try
            {
                _logger.LogInformation("Navigated to Login page.");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading Login page.");
                TempData["ErrorMessage"] = "An error occurred while loading the login page. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Initialize ViewBag properties
            ViewBag.InvalidLogin = false;
            ViewBag.UserNotFound = false;
            ViewBag.DatabaseError = false;
            ViewBag.UserLocked = false;

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for login attempt. UserName: {UserName}", model?.UserName ?? "Unknown");
                    return View(model);
                }

                _logger.LogInformation("Login attempt for user: {UserName}", model.UserName);

                // Check if user exists first
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {UserName}", model.UserName);
                    ViewBag.UserNotFound = true;
                    ViewBag.InvalidLogin = true;
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }

                // Check if user is locked out
                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("Login failed - user is locked out: {UserName}", model.UserName);
                    ViewBag.UserLocked = true;
                    ModelState.AddModelError(string.Empty, "Your account has been locked due to multiple failed login attempts. Please try again later.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in successfully: {UserName}", model.UserName);

                    try
                    {
                        // Redirect based on user role
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            _logger.LogInformation("Admin user logged in: {UserName}", model.UserName);
                            return RedirectToAction("AdminDashboard", "Admin");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Seller"))
                        {
                            return await HandleSellerLogin(user);
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Buyer"))
                        {
                            return await HandleBuyerLogin(user);
                        }
                        else
                        {
                            _logger.LogWarning("User role not recognized for user: {UserName}", model.UserName);
                            await _signInManager.SignOutAsync(); // Sign out user with invalid role
                            ModelState.AddModelError(string.Empty, "Your account role is not recognized. Please contact support.");
                            return View(model);
                        }
                    }
                    catch (Exception roleEx)
                    {
                        _logger.LogError(roleEx, "Error occurred while processing user role for: {UserName}", model.UserName);
                        await _signInManager.SignOutAsync(); // Sign out user due to role processing error
                        ModelState.AddModelError(string.Empty, "An error occurred while setting up your session. Please try again.");
                        return View(model);
                    }
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out: {UserName}", model.UserName);
                    ViewBag.UserLocked = true;
                    ModelState.AddModelError(string.Empty, "Your account has been locked due to multiple failed login attempts. Please try again later.");
                    return View(model);
                }
                else if (result.IsNotAllowed)
                {
                    _logger.LogWarning("User login not allowed: {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "Your account is not allowed to sign in. Please contact support.");
                    return View(model);
                }
                else
                {
                    _logger.LogWarning("Invalid login attempt for user: {UserName}", model.UserName);
                    ViewBag.InvalidLogin = true;
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred during login for user: {UserName}", model?.UserName ?? "Unknown");
                ViewBag.DatabaseError = true;
                ModelState.AddModelError(string.Empty, "A database error occurred. Please try again later.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during login for user: {UserName}", model?.UserName ?? "Unknown");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during login. Please try again later.");
                return View(model);
            }
        }

        /// <summary>
        /// Handles seller login and redirection
        /// </summary>
        private async Task<IActionResult> HandleSellerLogin(IdentityUser user)
        {
            try
            {
                bool sellerExists = await _context.Sellers.AnyAsync(s => s.UserId == user.Id);

                if (sellerExists)
                {
                    var sellerId = await _context.Sellers
                        .Where(s => s.UserId == user.Id)
                        .Select(s => s.SellerId)
                        .FirstOrDefaultAsync();

                    _logger.LogInformation("Existing seller logged in: {UserName}, SellerId: {SellerId}", user.UserName, sellerId);
                    return RedirectToAction("SellerDashboard", "Seller", new { sellerId });
                }
                else
                {
                    _logger.LogInformation("New seller logged in, redirecting to create seller details: {UserName}", user.UserName);
                    return RedirectToAction("CreateSellerDetails", "Seller");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling seller login for user: {UserName}", user.UserName);
                throw; // Re-throw to be caught by the main try-catch in Login method
            }
        }

        /// <summary>
        /// Handles buyer login and redirection
        /// </summary>
        private async Task<IActionResult> HandleBuyerLogin(IdentityUser user)
        {
            try
            {
                bool buyerExists = await _context.Buyers.AnyAsync(b => b.UserId == user.Id);

                if (buyerExists)
                {
                    var buyerId = await _context.Buyers
                        .Where(b => b.UserId == user.Id)
                        .Select(b => b.BuyerId)
                        .FirstOrDefaultAsync();

                    _logger.LogInformation("Existing buyer logged in: {UserName}, BuyerId: {BuyerId}", user.UserName, buyerId);
                    return RedirectToAction("BuyerDashboard", "Buyer", new { buyerId });
                }
                else
                {
                    _logger.LogInformation("New buyer logged in, redirecting to create buyer details: {UserName}", user.UserName);
                    return RedirectToAction("CreateBuyerDetails", "Buyer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling buyer login for user: {UserName}", user.UserName);
                throw; // Re-throw to be caught by the main try-catch in Login method
            }
        }

        // Admin Login action
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AdminLogin()
        {
            try
            {
                _logger.LogInformation("Navigated to AdminLogin page.");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading AdminLogin page.");
                TempData["ErrorMessage"] = "An error occurred while loading the admin login page. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AdminLogin(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for admin login attempt. UserName: {UserName}", model?.UserName ?? "Unknown");
                    return View(model);
                }

                _logger.LogInformation("Admin login attempt for user: {UserName}", model.UserName);

                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user == null)
                {
                    _logger.LogWarning("Admin login failed - user not found: {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
                    return View(model);
                }

                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    _logger.LogWarning("Admin login failed - user is not an admin: {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin logged in successfully: {UserName}", model.UserName);
                    return RedirectToAction("AdminDashboard", "Admin");
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("Admin account locked out: {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "Admin account has been locked. Please contact system administrator.");
                    return View(model);
                }
                else
                {
                    _logger.LogWarning("Invalid admin login attempt for user: {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during admin login for user: {UserName}", model?.UserName ?? "Unknown");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during admin login. Please try again later.");
                return View(model);
            }
        }

        // Logout
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userName = User?.Identity?.Name ?? "Unknown";
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out successfully: {UserName}", userName);
                TempData["SuccessMessage"] = "You have been logged out successfully.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout for user: {UserName}", User?.Identity?.Name ?? "Unknown");
                TempData["ErrorMessage"] = "An error occurred during logout. Your session may still be active.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
