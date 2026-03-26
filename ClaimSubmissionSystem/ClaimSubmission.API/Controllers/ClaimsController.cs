using Microsoft.AspNetCore.Mvc;
using ClaimSubmission.API.Data;
using ClaimSubmission.API.DTOs;

namespace ClaimSubmission.API.Controllers
{
    /// <summary>
    /// Claims API controller for managing claim submissions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimsRepository _repository;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(IClaimsRepository repository, ILogger<ClaimsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated claims with filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClaims([FromQuery] GetClaimsRequest request)
        {
            try
            {
                if (request.PageNumber <= 0 || request.PageSize <= 0)
                {
                    return BadRequest(new { error = "PageNumber and PageSize must be greater than 0" });
                }

                var result = await _repository.GetClaimsAsync(request);
                return Ok(new { data = result, message = "Claims retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving claims");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred while retrieving claims" });
            }
        }

        /// <summary>
        /// Get claim by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClaimById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid claim ID" });
                }

                var claim = await _repository.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    return NotFound(new { error = "Claim not found" });
                }

                var claimResponse = new ClaimResponse
                {
                    ClaimId = claim.ClaimId,
                    ClaimNumber = claim.ClaimNumber,
                    PatientName = claim.PatientName,
                    ProviderName = claim.ProviderName,
                    DateOfService = claim.DateOfService,
                    ClaimAmount = claim.ClaimAmount,
                    ClaimStatus = claim.ClaimStatus,
                    CreatedDate = claim.CreatedDate
                };

                return Ok(new { data = claimResponse, message = "Claim retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving claim {id}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred while retrieving the claim" });
            }
        }

        /// <summary>
        /// Create a new claim
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Claim data is required" });
                }

                // Validate mandatory fields
                var validationErrors = ValidateCreateClaimRequest(request);
                if (validationErrors.Count > 0)
                {
                    return BadRequest(new { errors = validationErrors });
                }

                // Extract userId from claims (placeholder - normally from JWT token)
                var userId = 1; // TODO: Get real user ID from claims

                var claimId = await _repository.CreateClaimAsync(request, userId);
                return Created($"/api/claims/{claimId}", new { 
                    data = new { claimId = claimId }, 
                    message = "Claim created successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred while creating the claim" });
            }
        }

        /// <summary>
        /// Update an existing claim
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateClaim(int id, [FromBody] UpdateClaimRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid claim ID" });
                }

                if (request == null)
                {
                    return BadRequest(new { error = "Claim data is required" });
                }

                // Validate mandatory fields
                var validationErrors = ValidateUpdateClaimRequest(request);
                if (validationErrors.Count > 0)
                {
                    return BadRequest(new { errors = validationErrors });
                }

                // Check if claim exists
                var existing = await _repository.GetClaimByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { error = "Claim not found" });
                }

                // Extract userId from claims (placeholder - normally from JWT token)
                var userId = 1; // TODO: Get real user ID from claims

                await _repository.UpdateClaimAsync(id, request, userId);
                return Ok(new { message = "Claim updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating claim {id}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred while updating the claim" });
            }
        }

        /// <summary>
        /// Delete a claim
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteClaim(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid claim ID" });
                }

                // Check if claim exists
                var existing = await _repository.GetClaimByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { error = "Claim not found" });
                }

                await _repository.DeleteClaimAsync(id);
                return Ok(new { message = "Claim deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting claim {id}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred while deleting the claim" });
            }
        }

        /// <summary>
        /// Validate CreateClaimRequest fields
        /// </summary>
        private List<string> ValidateCreateClaimRequest(CreateClaimRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.ClaimNumber))
                errors.Add("Claim Number is mandatory");
            if (string.IsNullOrWhiteSpace(request.PatientName))
                errors.Add("Patient Name is mandatory");
            if (string.IsNullOrWhiteSpace(request.ProviderName))
                errors.Add("Provider Name is mandatory");
            if (request.DateOfService == default)
                errors.Add("Date of Service is mandatory");
            if (request.ClaimAmount <= 0)
                errors.Add("Claim Amount must be greater than 0");
            if (string.IsNullOrWhiteSpace(request.ClaimStatus))
                errors.Add("Claim Status is mandatory");

            return errors;
        }

        /// <summary>
        /// Validate UpdateClaimRequest fields
        /// </summary>
        private List<string> ValidateUpdateClaimRequest(UpdateClaimRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.PatientName))
                errors.Add("Patient Name is mandatory");
            if (string.IsNullOrWhiteSpace(request.ProviderName))
                errors.Add("Provider Name is mandatory");
            if (request.DateOfService == default)
                errors.Add("Date of Service is mandatory");
            if (request.ClaimAmount <= 0)
                errors.Add("Claim Amount must be greater than 0");
            if (string.IsNullOrWhiteSpace(request.ClaimStatus))
                errors.Add("Claim Status is mandatory");

            return errors;
        }
    }
}

