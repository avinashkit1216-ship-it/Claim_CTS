// ImprovedClaimsController.cs - Refactored API Controller
// This replaces the existing ClaimsController.cs in ClaimSubmission.API/Controllers

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using ClaimSubmission.API.Common;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Services;

namespace ClaimSubmission.API.Controllers
{
    /// <summary>
    /// Claims API controller for managing claim submissions
    /// All endpoints require JWT authentication
    /// Rate limited to 100 requests per minute
    /// </summary>
    [ApiController]
    [Route("api/improvedclaims")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class ImprovedClaimsController : ControllerBase
    {
        private readonly IClaimService _service;
        private readonly ILogger<ImprovedClaimsController> _logger;

        public ImprovedClaimsController(IClaimService service, ILogger<ImprovedClaimsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated claims with optional filtering
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Records per page (default: 20, max: 500)</param>
        /// <param name="searchTerm">Optional search term for claim number or patient name</param>
        /// <param name="claimStatus">Optional filter by claim status</param>
        /// <param name="sortBy">Field to sort by (default: CreatedDate)</param>
        /// <param name="sortDirection">Sort direction ASC/DESC (default: DESC)</param>
        /// <returns>Paginated list of claims</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult<PaginatedClaimsResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OperationResult))]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetClaims(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? claimStatus = null,
            [FromQuery] string? sortBy = "CreatedDate",
            [FromQuery] string? sortDirection = "DESC")
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId <= 0)
                {
                    return Unauthorized(new { error = "Invalid user context" });
                }

                var request = new GetClaimsRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    ClaimStatus = claimStatus,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                };

                var result = await _service.GetClaimsAsync(request, userId);
                
                if (!result.IsSuccess)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetClaims");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    OperationResult.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Get claim details by ID
        /// </summary>
        /// <param name="id">Claim ID</param>
        /// <returns>Claim details</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult<ClaimResponse>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(OperationResult))]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetClaimById([FromRoute] int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                
                var result = await _service.GetClaimByIdAsync(id, userId);
                
                if (!result.IsSuccess)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetClaimById");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    OperationResult.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Create a new claim
        /// </summary>
        /// <param name="request">Claim details</param>
        /// <returns>Created claim ID</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OperationResult<int>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OperationResult))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(OperationResult))]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                
                var result = await _service.CreateClaimAsync(request, userId);
                
                if (!result.IsSuccess)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return CreatedAtAction(nameof(GetClaimById), new { id = result.Data }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateClaim");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    OperationResult.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Update an existing claim
        /// </summary>
        /// <param name="id">Claim ID</param>
        /// <param name="request">Updated claim details</param>
        /// <returns>Success response</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OperationResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(OperationResult))]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> UpdateClaim([FromRoute] int id, [FromBody] UpdateClaimRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                
                var result = await _service.UpdateClaimAsync(id, request, userId);
                
                if (!result.IsSuccess)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateClaim");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    OperationResult.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Delete a claim
        /// </summary>
        /// <param name="id">Claim ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(OperationResult))]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> DeleteClaim([FromRoute] int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                
                var result = await _service.DeleteClaimAsync(id, userId);
                
                if (!result.IsSuccess)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteClaim");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    OperationResult.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Health check endpoint (no auth required)
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
