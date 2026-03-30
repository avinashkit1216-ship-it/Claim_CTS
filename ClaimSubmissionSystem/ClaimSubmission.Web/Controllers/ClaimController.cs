using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClaimSubmission.Web.Models;
using ClaimSubmission.Web.Services;

namespace ClaimSubmission.Web.Controllers
{
    /// <summary>
    /// Claims controller for managing claims
    /// User must be authenticated to access these endpoints
    /// </summary>
    public class ClaimController : Controller
    {
        private readonly IClaimApiService _claimApiService;
        private readonly ILogger<ClaimController> _logger;

        public ClaimController(IClaimApiService claimApiService, ILogger<ClaimController> logger)
        {
            _claimApiService = claimApiService;
            _logger = logger;
        }

        /// <summary>
        /// Display paginated list of claims with filtering (alias for Index)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List(int pageNumber = 1, int pageSize = 20, 
            string? searchTerm = null, string? claimStatus = null, 
            string? sortBy = "CreatedDate", string? sortDirection = "DESC")
        {
            try
            {
                if (!IsUserAuthenticated())
                    return RedirectToAction("Login", "Authentication");

                // Ensure page parameters are valid
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 20;

                string token = GetUserToken();
                
                var claims = await _claimApiService.GetClaimsAsync(token, pageNumber, pageSize, 
                    searchTerm, claimStatus, sortBy, sortDirection);

                if (claims == null)
                {
                    claims = new ClaimsPaginatedListViewModel 
                    { 
                        Claims = new List<ClaimViewListModel>(),
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };
                    ViewBag.Message = "No claims found.";
                }

                return View(claims);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access attempt");
                return RedirectToAction("Login", "Authentication");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claims");
                ViewBag.Error = "An error occurred while loading claims. Please try again.";
                return View(new ClaimsPaginatedListViewModel 
                { 
                    Claims = new List<ClaimViewListModel>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
        }

        /// <summary>
        /// Display paginated list of claims with filtering
        /// </summary>
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20, 
            string? searchTerm = null, string? claimStatus = null, 
            string? sortBy = "CreatedDate", string? sortDirection = "DESC")
        {
            try
            {
                if (!IsUserAuthenticated())
                    return RedirectToAction("Login", "Authentication");

                // Ensure page parameters are valid
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 20;

                string token = GetUserToken();
                
                var claims = await _claimApiService.GetClaimsAsync(token, pageNumber, pageSize, 
                    searchTerm, claimStatus, sortBy, sortDirection);

                if (claims == null)
                {
                    claims = new ClaimsPaginatedListViewModel 
                    { 
                        Claims = new List<ClaimViewListModel>(),
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };
                    ViewBag.Message = "No claims found.";
                }

                return View(claims);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access attempt");
                return RedirectToAction("Login", "Authentication");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claims");
                ViewBag.Error = "An error occurred while loading claims. Please try again.";
                return View(new ClaimsPaginatedListViewModel 
                { 
                    Claims = new List<ClaimViewListModel>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
        }

        /// <summary>
        /// Display add claim page (alias for Create)
        /// </summary>
        [HttpGet]
        public IActionResult Add()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login", "Authentication");

            return View(new AddClaimViewModel());
        }

        /// <summary>
        /// Handle claim addition (alias for Create)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddClaimViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login", "Authentication");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string token = GetUserToken();
                // Convert AddClaimViewModel to CreateClaimViewModel for API call
                var createModel = new CreateClaimViewModel
                {
                    ClaimNumber = model.ClaimNumber,
                    PatientName = model.PatientName,
                    ProviderName = model.ProviderName,
                    DateOfService = model.DateOfService,
                    ClaimAmount = model.ClaimAmount,
                    ClaimStatus = model.ClaimStatus
                };

                int claimId = await _claimApiService.CreateClaimAsync(token, createModel);

                if (claimId > 0)
                {
                    _logger.LogInformation($"Claim created successfully - ID: {claimId}");
                    TempData["SuccessMessage"] = "Claim created successfully";
                    return RedirectToAction(nameof(List));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to create claim");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                ModelState.AddModelError(string.Empty, $"Error creating claim: {ex.Message}");
            }

            return View(model);
        }

        /// <summary>
        /// Display create claim page
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login", "Authentication");

            return View(new CreateClaimViewModel());
        }

        /// <summary>
        /// Handle claim creation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClaimViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login", "Authentication");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string token = GetUserToken();
                int claimId = await _claimApiService.CreateClaimAsync(token, model);

                if (claimId > 0)
                {
                    _logger.LogInformation($"Claim created successfully - ID: {claimId}");
                    TempData["SuccessMessage"] = "Claim created successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to create claim");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                ModelState.AddModelError(string.Empty, $"Error creating claim: {ex.Message}");
            }

            return View(model);
        }

        /// <summary>
        /// Display edit claim page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login", "Authentication");

            if (id <= 0)
                return NotFound();

            try
            {
                string token = GetUserToken();
                var claim = await _claimApiService.GetClaimByIdAsync(token, id);

                if (claim == null)
                    return NotFound();

                return View(claim);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Authentication");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading claim {id}");
                return NotFound();
            }
        }

        /// <summary>
        /// Handle claim update
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditClaimViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login", "Authentication");

            if (id != model.ClaimId)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string token = GetUserToken();
                await _claimApiService.UpdateClaimAsync(token, model);

                _logger.LogInformation($"Claim updated successfully - ID: {id}");
                TempData["SuccessMessage"] = "Claim updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Authentication");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating claim {id}");
                ModelState.AddModelError(string.Empty, $"Error updating claim: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// Handle claim deletion
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login", "Authentication");

            if (id <= 0)
                return BadRequest();

            try
            {
                string token = GetUserToken();
                await _claimApiService.DeleteClaimAsync(token, id);

                _logger.LogInformation($"Claim deleted successfully - ID: {id}");
                TempData["SuccessMessage"] = "Claim deleted successfully";
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Authentication");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting claim {id}");
                TempData["ErrorMessage"] = $"Error deleting claim: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        private bool IsUserAuthenticated()
        {
            return HttpContext.Session.GetString("IsAuthenticated") == "true";
        }

        /// <summary>
        /// Get user authentication token from session
        /// </summary>
        private string GetUserToken()
        {
            return HttpContext.Session.GetString("UserToken") ?? string.Empty;
        }
    }
}
