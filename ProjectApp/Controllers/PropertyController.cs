using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectApp.Models;
using ProjectApp.ViewModels;
using ProjectApp.Data;
using System.Text;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data.Common;

namespace ProjectApp.Controllers
{
    /// <summary>
    /// This class is used to create a property by giving the required data
    /// </summary>
    public class PropertyController : Controller
    {
        private readonly AppsDbContext _context;
        private readonly ILogger<PropertyController> _logger;
        private readonly string _azureBlobStorageConnectionString;
        public PropertyController(AppsDbContext context, ILogger<PropertyController> logger,IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _azureBlobStorageConnectionString = configuration.GetConnectionString("AzureBlobStorage");
        }

        // GET: Property/Create
        public async Task<IActionResult> Create(int sellerId)
        {
            _logger.LogInformation("Fetching data for Create action with SellerId: {SellerId}", sellerId);

            try
            {
                var states = await _context.States.ToListAsync();
                var cities = await _context.Cities.ToListAsync();

                // Fetch seller with the given sellerId
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.SellerId == sellerId);

                if (seller == null)
                {
                    _logger.LogWarning("Seller not found with SellerId: {SellerId}", sellerId);
                    return NotFound();
                }

                var sellerEmail = seller.EmailId;

                var model = new PropertyViewModel
                {
                    SellerId = sellerId,
                    States = new SelectList(states, "StateId", "StateName"),
                    Cities = new SelectList(cities, "CityId", "CityName"),
                    SellerEmailId = sellerEmail,
                    PropertyOptions = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = "Rent", Text = "Rent" },
                new SelectListItem { Value = "Sell", Text = "Sell" }
            }, "Value", "Text")
                };

                _logger.LogInformation("Create action data fetched successfully for SellerId: {SellerId}", sellerId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching data for Create action with SellerId: {SellerId}", sellerId);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: Property/Create
        [HttpPost]
        public async Task<IActionResult> Create(PropertyViewModel model, IList<IFormFile> images)
        {
            _logger.LogInformation("Create POST action started for SellerId: {SellerId}", model.SellerId);

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if State exists, if not create it
                    var state = await _context.States
                        .FirstOrDefaultAsync(s => s.StateName == model.StateName);

                    // Get StateId
                    int stateId = state?.StateId ?? model.StateId;

                    // Check if City exists in the given State, if not create it
                    var city = await _context.Cities
                        .FirstOrDefaultAsync(c => c.CityName == model.CityName && c.StateId == stateId);

                    if (city == null && !string.IsNullOrEmpty(model.CityName))
                    {
                        _logger.LogInformation("Creating new City with name: {CityName} and StateId: {StateId}", model.CityName, stateId);
                        city = new City { CityName = model.CityName, StateId = stateId };
                        _context.Cities.Add(city);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("City created with Id: {CityId}", city.CityId);
                    }

                    // Get CityId
                    int? cityId = city?.CityId ?? model.CityId;

                    // Handle Property creation
                    var property = new Property
                    {
                        PropertyName = model.PropertyName,
                        PropertyType = model.PropertyType,
                        PropertyOption = model.PropertyOption,
                        Description = model.Description,
                        Address = model.Address,
                        PriceRange = model.PriceRange,
                        InitialDeposit = model.PropertyOption == "Rent" ? model.InitialDeposit : 0,
                        Landmark = model.Landmark,
                        IsActive = false, // Set by admin
                        SellerId = model.SellerId, // Use selected SellerId
                        CityId = city != null ? city.CityId : model.CityId,
                    };

                    _logger.LogInformation("Adding new Property with Name: {PropertyName}", property.PropertyName);
                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Property created with Id: {PropertyId}", property.PropertyId);

                    // Handle images upload logic
                    if (images != null && images.Any())
                    {
                        foreach (var image in images)
                        {
                            if (image.Length > 0)
                            {
                                using (var ms = new MemoryStream())
                                {
                                    await image.CopyToAsync(ms);
                                    var propertyImage = new Image
                                    {
                                        PropertyId = property.PropertyId,
                                        ImageData = ms.ToArray()
                                    };
                                    _context.Images.Add(propertyImage);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Images uploaded for PropertyId: {PropertyId}", property.PropertyId);
                    }

                    var propertyViewModel = new PropertyViewModel
                    {
                        PropertyId = property.PropertyId,
                        PropertyName = property.PropertyName,
                        PropertyType = property.PropertyType,
                        PropertyOption = property.PropertyOption,
                        Description = property.Description,
                        Address = property.Address,
                        PriceRange = property.PriceRange,
                        InitialDeposit = property.PropertyOption == "Rent" ? model.InitialDeposit : 0,
                        Landmark = property.Landmark,
                        IsActive = false, // Set by admin
                        SellerId = property.SellerId, // Use selected SellerId
                        CityId = city != null ? city.CityId : property.CityId,
                        StateId = stateId,
                        SellerEmailId = model.SellerEmailId,
                        Images = property.Images
                    };

                    var settings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Resolves JSON circular references issue
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                    string propertyJsonStr = JsonConvert.SerializeObject(propertyViewModel, settings);
                    string conStr = _azureBlobStorageConnectionString;

                    try
                    {
                        _logger.LogInformation("Uploading property details to Blob Storage.");
                        // Call the upload method to replace the blob content
                        UploadBlob(conStr, propertyJsonStr, "ezhousing");
                        ViewBag.MessageToScreent = "Details Updated to Blob: " + propertyJsonStr;
                        _logger.LogInformation("Property details uploaded to Blob Storage successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update blob with property details.");
                        ViewBag.MessageToScreent = "Failed to update blob: " + ex.Message;
                    }

                    return RedirectToAction("SellerDashboard", "Seller", new { sellerId = property.SellerId });
                }
                catch (DbException ex)
                {
                    _logger.LogError(ex, "Database update error occurred while creating the property.");
                    ModelState.AddModelError("", "An error occurred while saving your property. Please try again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while creating the property.");
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }

                // Reload dropdowns if model state is invalid
                var states = await _context.States.ToListAsync();
                var cities = await _context.Cities.ToListAsync();

                model.States = new SelectList(states, "StateId", "StateName");
                model.Cities = new SelectList(cities, "CityId", "CityName");
                model.PropertyOptions = new SelectList(new List<SelectListItem>
        {
            new SelectListItem { Value = "Rent", Text = "Rent" },
            new SelectListItem { Value = "Sell", Text = "Sell" }
        }, "Value", "Text");

                return View(model);
            }

            _logger.LogWarning("Model state is invalid for Create POST action.");
            return View(model);
        }
        private static void SetVariables(string conStr, string containerName, out string fileName, out string existingContent, out BlobClient blobClient)
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("BlobClientSetup");

            logger.LogInformation("Setting up BlobClient. Container: {ContainerName}", containerName);

            var serviceClient = new BlobServiceClient(conStr);
            var containerClient = serviceClient.GetBlobContainerClient(containerName);

            fileName = "data.txt";
            existingContent = "";
            blobClient = containerClient.GetBlobClient(fileName);

            logger.LogInformation("BlobClient setup completed. FileName: {FileName}", fileName);
        }

        public static string UploadBlob(string conStr, string fileContent, string containerName, bool isAppend = false)
        {
            // Initialize logger (you might want to inject a logger instance if it's a class method)
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("BlobUploader");

            string result = "Success";
            try
            {
                logger.LogInformation("UploadBlob started. Container: {ContainerName}, IsAppend: {IsAppend}", containerName, isAppend);

                string fileName, existingContent;
                BlobClient blobClient;

                SetVariables(conStr, containerName, out fileName, out existingContent, out blobClient);

                logger.LogInformation("BlobClient initialized for file: {FileName}", fileName);

                
                    using (var ms = new MemoryStream())
                    {
                        using (var tw = new StreamWriter(ms))
                        {
                            tw.Write(fileContent);
                            tw.Flush();
                            ms.Position = 0;

                            blobClient.Upload(ms, true);
                            logger.LogInformation("Blob content uploaded.");
                        }
                    }
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload blob. Exception: {ExceptionMessage}", ex.Message);
                result = "Failed";
            }

            logger.LogInformation("UploadBlob completed with result: {Result}", result);
            return result;
        }

        public async Task<IActionResult> VerifiedProperties(int sellerId)
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("VerifiedProperties");

            logger.LogInformation("VerifiedProperties action started for SellerId: {SellerId}", sellerId);

            try
            {
                var properties = await _context.Properties
                    .Where(p => p.SellerId == sellerId && p.IsActive)
                    .ToListAsync();

                logger.LogInformation("Verified properties retrieved successfully for SellerId: {SellerId}", sellerId);

                ViewBag.SellerId = sellerId;
                return View("Index", properties); // Reuse the same view to display properties
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving verified properties for SellerId: {SellerId}. Exception: {ExceptionMessage}", sellerId, ex.Message);
                // Handle exception and return an appropriate view or error message
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<IActionResult> DeactivatedProperties(int sellerId)
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("DeactivatedProperties");

            logger.LogInformation("DeactivatedProperties action started for SellerId: {SellerId}", sellerId);

            try
            {
                var properties = await _context.Properties
                    .Where(p => p.SellerId == sellerId && !p.IsActive)
                    .ToListAsync();

                logger.LogInformation("Deactivated properties retrieved successfully for SellerId: {SellerId}", sellerId);

                ViewBag.SellerId = sellerId;
                return View("Index", properties); // Reuse the same view to display properties
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving deactivated properties for SellerId: {SellerId}. Exception: {ExceptionMessage}", sellerId, ex.Message);
                // Handle exception and return an appropriate view or error message
                return StatusCode(500, "Internal server error");
            }
        }



        // GET: Property/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                int sellerId = _context.Properties
                                        .Where(p => p.PropertyId == id)
                                        .Select(p => p.SellerId).ToList()[0];

                string sellerEmail = _context.Sellers
                                        .Where(s => s.SellerId == sellerId)
                                        .Select(s => s.EmailId).ToList()[0];

                // Fetch the property along with City and Seller details
                var property = await _context.Properties
                    .Include(p => p.City)
                    .ThenInclude(c => c.State) // Include the State related to the City
                    .Include(p => p.Seller)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.PropertyId == id);

                if (property == null)
                {
                    return NotFound();
                }


                // Fetch the list of states and cities for dropdowns
                var states = await _context.States.ToListAsync();
                var cities = await _context.Cities.ToListAsync();

                // Map images to Base64 strings
                var imageBase64Strings = property.Images
                    .Select(img => Convert.ToBase64String(img.ImageData))
                    .ToList();

                // Initialize the PropertyViewModel with the necessary details
                var model = new PropertyViewModel
                {
                    PropertyId = property.PropertyId,
                    PropertyName = property.PropertyName,
                    PropertyType = property.PropertyType,
                    PropertyOption = property.PropertyOption,
                    Description = property.Description,
                    Address = property.Address,
                    PriceRange = property.PriceRange,
                    InitialDeposit = property.InitialDeposit ?? 0,
                    Landmark = property.Landmark,
                    IsActive = property.IsActive,
                    StateId = property.City.StateId,
                    CityId = property.CityId,
                    SellerId = property.SellerId,
                    StateName = property.City.State?.StateName,
                    CityName = property.City.CityName,
                    SellerEmailId = sellerEmail,
                    States = new SelectList(states, "StateId", "StateName"),
                    Cities = new SelectList(cities, "CityId", "CityName"),
                    PropertyOptions = new SelectList(new List<SelectListItem>
                    {
                        new SelectListItem { Value = "Rent", Text = "Rent" },
                        new SelectListItem { Value = "Sell", Text = "Sell" }
                    }, "Value", "Text"),
                    Images = property.Images,
                    ImageBase64Strings = imageBase64Strings
                };

                return View(model);
            }
            catch (DbException ex)
            {
                // Log the database update exception
                _logger.LogError(ex, "Database update error occurred while creating the property.");

                // Display error message to user
                ModelState.AddModelError("", "An error occurred while saving your property. Please try again.");
            }

            return View();
        }

        // POST: Property/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(PropertyViewModel model, IEnumerable<IFormFile> images)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Starting property edit process for PropertyId: {PropertyId}", model.PropertyId);

                    // Fetch the existing property
                    var property = await _context.Properties
                        .Include(p => p.Images) // Include images if needed
                        .FirstOrDefaultAsync(p => p.PropertyId == model.PropertyId);

                    if (property == null)
                    {
                        _logger.LogWarning("Property with PropertyId: {PropertyId} not found.", model.PropertyId);
                        return NotFound();
                    }

                    // Get StateId
                    int stateId = model.StateId;

                    // Check if City exists in the given State, if not create it
                    var city = await _context.Cities
                          .FirstOrDefaultAsync(c => c.CityId == model.CityId && c.StateId == stateId);

                    if (city == null && !string.IsNullOrEmpty(model.CityName))
                    {
                        city = new City { CityName = model.CityName, StateId = stateId };
                        _context.Cities.Add(city);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Created new city: {CityName} in StateId: {StateId}", model.CityName, stateId);
                    }

                    // Update property details
                    property.PropertyName = model.PropertyName;
                    property.PropertyType = model.PropertyType;
                    property.PropertyOption = model.PropertyOption;
                    property.Description = model.Description;
                    property.Address = model.Address;
                    property.PriceRange = model.PriceRange;
                    property.InitialDeposit = model.PropertyOption == "Rent" ? model.InitialDeposit : 0;
                    property.Landmark = model.Landmark;
                    property.IsActive = model.IsActive;
                    property.SellerId = model.SellerId; // Use selected SellerId
                    property.CityId = city != null ? city.CityId : model.CityId;

                    // Handle image uploads if images are provided
                    if (images != null && images.Any())
                    {
                        foreach (var image in images)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await image.CopyToAsync(memoryStream);
                                var imageData = memoryStream.ToArray();

                                var imageEntity = new Image
                                {
                                    PropertyId = property.PropertyId,
                                    ImageData = imageData,
                                };

                                _context.Images.Add(imageEntity);
                            }
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Uploaded {ImageCount} images for PropertyId: {PropertyId}", images.Count(), property.PropertyId);
                    }

                    _context.Properties.Update(property);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated property details for PropertyId: {PropertyId}", property.PropertyId);

                    var propertyViewModel = new PropertyViewModel
                    {
                        PropertyId = property.PropertyId,
                        PropertyName = property.PropertyName,
                        PropertyType = property.PropertyType,
                        PropertyOption = property.PropertyOption,
                        Description = property.Description,
                        Address = property.Address,
                        PriceRange = property.PriceRange,
                        InitialDeposit = property.PropertyOption == "Rent" ? model.InitialDeposit : 0,
                        Landmark = property.Landmark,
                        IsActive = false, // Set by admin
                        SellerId = property.SellerId, // Use selected SellerId
                        CityId = city != null ? city.CityId : property.CityId,
                        StateId = stateId,
                        SellerEmailId = model.SellerEmailId,
                        Images = property.Images
                    };

                    var settings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                    string propertyJsonStr = JsonConvert.SerializeObject(propertyViewModel, settings);
                    string conStr = _azureBlobStorageConnectionString;

                    try
                    {
                        // Call the upload method to replace the blob content
                        UploadBlob(conStr, propertyJsonStr, "ezhousing");
                        ViewBag.MessageToScreent = "Details Updated to Blob: " + propertyJsonStr;
                        _logger.LogInformation("Property details uploaded to Blob Storage: {BlobContent}", propertyJsonStr);
                    }
                    catch (Exception ex)
                    {
                        ViewBag.MessageToScreent = "Failed to update blob: " + ex.Message;
                        _logger.LogError(ex, "Failed to update Blob Storage with Property details for PropertyId: {PropertyId}", property.PropertyId);
                    }
                }
                catch (DbException ex)
                {
                    _logger.LogError(ex, "Database update error occurred while editing PropertyId: {PropertyId}", model.PropertyId);
                    ModelState.AddModelError("", "An error occurred while saving your property. Please try again.");
                }

                return RedirectToAction("SellerDashboard", "Seller", new { sellerId = model.SellerId });
            }

            // Reload dropdowns if model state is invalid
            var states = await _context.States.ToListAsync();
            var cities = await _context.Cities.ToListAsync();

            model.States = new SelectList(states, "StateId", "StateName");
            model.Cities = new SelectList(cities, "CityId", "CityName");
            model.PropertyOptions = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = "Rent", Text = "Rent" },
                new SelectListItem { Value = "Sell", Text = "Sell" }
            }, "Value", "Text");

            return View(model);
        }
    }
}
