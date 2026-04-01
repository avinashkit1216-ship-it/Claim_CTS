// ImprovedClaimService.cs - Service Layer with Caching & Validation
using ClaimSubmission.API.Common;
using ClaimSubmission.API.Data;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Models;
using FluentValidation;

namespace ClaimSubmission.API.Services
{
    /// <summary>
    /// Business layer for claims operations
    /// Handles validation, caching, and orchestration
    /// </summary>
    public interface IClaimService
    {
        Task<OperationResult<PaginatedClaimsResponse>> GetClaimsAsync(GetClaimsRequest request, int userId);
        Task<OperationResult<ClaimResponse>> GetClaimByIdAsync(int claimId, int userId);
        Task<OperationResult<int>> CreateClaimAsync(CreateClaimRequest request, int userId);
        Task<OperationResult> UpdateClaimAsync(int claimId, UpdateClaimRequest request, int userId);
        Task<OperationResult> DeleteClaimAsync(int claimId, int userId);
    }

    public class ClaimService : IClaimService
    {
        private readonly IClaimsRepository _repository;
        private readonly ICacheService _cache;
        private readonly IValidator<CreateClaimRequest> _createValidator;
        private readonly IValidator<UpdateClaimRequest> _updateValidator;
        private readonly IValidator<GetClaimsRequest> _getValidator;
        private readonly ILogger<ClaimService> _logger;

        public ClaimService(
            IClaimsRepository repository,
            ICacheService cache,
            IValidator<CreateClaimRequest> createValidator,
            IValidator<UpdateClaimRequest> updateValidator,
            IValidator<GetClaimsRequest> getValidator,
            ILogger<ClaimService> logger)
        {
            _repository = repository;
            _cache = cache;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _getValidator = getValidator;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated claims with caching
        /// </summary>
        public async Task<OperationResult<PaginatedClaimsResponse>> GetClaimsAsync(
            GetClaimsRequest request, int userId)
        {
            try
            {
                // Validate request
                var validation = await _getValidator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Invalid GetClaims request: {errors}");
                    return OperationResult<PaginatedClaimsResponse>.Failure(errors, 
                        StatusCodes.Status400BadRequest);
                }

                // Generate cache key
                var cacheKey = $"claims_page_{request.PageNumber}_{request.PageSize}_{request.SearchTerm}_{request.ClaimStatus}";

                // Try cache first
                var cached = await _cache.GetAsync<PaginatedClaimsResponse>(cacheKey);
                if (cached != null)
                {
                    _logger.LogInformation($"Claims retrieved from cache for user {userId}");
                    return OperationResult<PaginatedClaimsResponse>.Success(cached);
                }

                // Get from repository
                var result = await _repository.GetClaimsAsync(request);

                if (result == null || result.Claims == null)
                {
                    return OperationResult<PaginatedClaimsResponse>.Failure(
                        "No claims found", StatusCodes.Status404NotFound);
                }

                // Cache the result
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

                _logger.LogInformation($"Retrieved {result.Claims.Count} claims for user {userId}");
                return OperationResult<PaginatedClaimsResponse>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving claims for user {userId}");
                return OperationResult<PaginatedClaimsResponse>.Failure(
                    "An error occurred while retrieving claims",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get single claim by ID with caching
        /// </summary>
        public async Task<OperationResult<ClaimResponse>> GetClaimByIdAsync(int claimId, int userId)
        {
            try
            {
                if (claimId <= 0)
                {
                    return OperationResult<ClaimResponse>.Failure(
                        "Invalid claim ID", StatusCodes.Status400BadRequest);
                }

                var cacheKey = $"claim_{claimId}";
                var cached = await _cache.GetAsync<ClaimResponse>(cacheKey);
                if (cached != null)
                {
                    _logger.LogDebug($"Claim {claimId} retrieved from cache");
                    return OperationResult<ClaimResponse>.Success(cached);
                }

                var claim = await _repository.GetClaimByIdAsync(claimId);
                if (claim == null)
                {
                    _logger.LogWarning($"Claim {claimId} not found for user {userId}");
                    return OperationResult<ClaimResponse>.Failure(
                        "Claim not found", StatusCodes.Status404NotFound);
                }

                var response = MapToResponse(claim);
                await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));

                return OperationResult<ClaimResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving claim {claimId}");
                return OperationResult<ClaimResponse>.Failure(
                    "An error occurred while retrieving the claim",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Create new claim with validation
        /// </summary>
        public async Task<OperationResult<int>> CreateClaimAsync(CreateClaimRequest request, int userId)
        {
            try
            {
                // Validate request
                var validation = await _createValidator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Invalid CreateClaim request from user {userId}: {errors}");
                    return OperationResult<int>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                // Check for duplicate claim number
                var existing = await _repository.GetClaimByNumberAsync(request.ClaimNumber);
                if (existing != null)
                {
                    return OperationResult<int>.Failure(
                        $"Claim number {request.ClaimNumber} already exists",
                        StatusCodes.Status409Conflict);
                }

                // Create claim
                var claimId = await _repository.CreateClaimAsync(request, userId);

                // Invalidate cache
                await _cache.RemoveAsync("claims_list");

                _logger.LogInformation($"Claim {claimId} created by user {userId}");
                return OperationResult<int>.Success(claimId, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating claim for user {userId}");
                return OperationResult<int>.Failure(
                    "An error occurred while creating the claim",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update existing claim with validation
        /// </summary>
        public async Task<OperationResult> UpdateClaimAsync(int claimId, UpdateClaimRequest request, int userId)
        {
            try
            {
                if (claimId <= 0)
                {
                    return OperationResult.Failure("Invalid claim ID", StatusCodes.Status400BadRequest);
                }

                // Validate request
                var validation = await _updateValidator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                    return OperationResult.Failure(errors, StatusCodes.Status400BadRequest);
                }

                // Check if claim exists
                var existing = await _repository.GetClaimByIdAsync(claimId);
                if (existing == null)
                {
                    return OperationResult.Failure("Claim not found", StatusCodes.Status404NotFound);
                }

                // Update claim
                await _repository.UpdateClaimAsync(claimId, request, userId);

                // Invalidate cache
                await _cache.RemoveAsync($"claim_{claimId}");
                await _cache.RemoveAsync("claims_list");

                _logger.LogInformation($"Claim {claimId} updated by user {userId}");
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating claim {claimId}");
                return OperationResult.Failure(
                    "An error occurred while updating the claim",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete claim (soft delete recommended)
        /// </summary>
        public async Task<OperationResult> DeleteClaimAsync(int claimId, int userId)
        {
            try
            {
                if (claimId <= 0)
                {
                    return OperationResult.Failure("Invalid claim ID", StatusCodes.Status400BadRequest);
                }

                var existing = await _repository.GetClaimByIdAsync(claimId);
                if (existing == null)
                {
                    return OperationResult.Failure("Claim not found", StatusCodes.Status404NotFound);
                }

                await _repository.DeleteClaimAsync(claimId);

                // Invalidate cache
                await _cache.RemoveAsync($"claim_{claimId}");
                await _cache.RemoveAsync("claims_list");

                _logger.LogInformation($"Claim {claimId} deleted by user {userId}");
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting claim {claimId}");
                return OperationResult.Failure(
                    "An error occurred while deleting the claim",
                    StatusCodes.Status500InternalServerError);
            }
        }

        private ClaimResponse MapToResponse(Claim claim) => new()
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
    }
}
