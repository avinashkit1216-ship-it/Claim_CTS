using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Models;

namespace ClaimSubmission.API.Data.LocalStorage
{
    /// <summary>
    /// Claims repository implementation using local storage (JSON files)
    /// </summary>
    public class LocalClaimsRepository : IClaimsRepository
    {
        private readonly LocalStorageService _storageService;
        private readonly ILogger<LocalClaimsRepository> _logger;
        private const string CLAIMS_FILE = "claims.json";

        public LocalClaimsRepository(LocalStorageService storageService, ILogger<LocalClaimsRepository> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<Claim?> GetClaimByIdAsync(int claimId)
        {
            try
            {
                _logger.LogDebug($"Retrieving claim with ID: {claimId}");
                return await _storageService.ReadByIdAsync<Claim>(CLAIMS_FILE, claimId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving claim {claimId}");
                throw;
            }
        }

        public async Task<Claim?> GetClaimByNumberAsync(string claimNumber)
        {
            try
            {
                _logger.LogDebug($"Retrieving claim with number: {claimNumber}");
                var claims = await _storageService.ReadAllAsync<Claim>(CLAIMS_FILE);
                return claims.FirstOrDefault(c => c.ClaimNumber?.Equals(claimNumber, StringComparison.OrdinalIgnoreCase) == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving claim {claimNumber}");
                throw;
            }
        }

        public async Task<PaginatedClaimsResponse> GetClaimsAsync(GetClaimsRequest request)
        {
            try
            {
                _logger.LogDebug($"Retrieving claims - Page: {request.PageNumber}, PageSize: {request.PageSize}");
                
                var claims = await _storageService.ReadAllAsync<Claim>(CLAIMS_FILE);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchLower = request.SearchTerm.ToLower();
                    claims = claims.Where(c =>
                        (c.PatientName?.ToLower().Contains(searchLower) ?? false) ||
                        (c.ProviderName?.ToLower().Contains(searchLower) ?? false) ||
                        (c.ClaimNumber?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();
                }

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(request.ClaimStatus))
                {
                    claims = claims.Where(c => 
                        c.ClaimStatus?.Equals(request.ClaimStatus, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                // Apply sorting
                claims = ApplySorting(claims, request.SortBy, request.SortDirection);

                // Calculate pagination
                int totalRecords = claims.Count;
                var paginatedClaims = claims
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(c => new ClaimResponse
                    {
                        ClaimId = c.ClaimId,
                        ClaimNumber = c.ClaimNumber,
                        PatientName = c.PatientName,
                        ProviderName = c.ProviderName,
                        DateOfService = c.DateOfService,
                        ClaimAmount = c.ClaimAmount,
                        ClaimStatus = c.ClaimStatus,
                        CreatedDate = c.CreatedDate
                    })
                    .ToList();

                return new PaginatedClaimsResponse
                {
                    Claims = paginatedClaims,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving claims");
                throw;
            }
        }

        public async Task<int> GetClaimsCountAsync(string? searchTerm = null, string? claimStatus = null)
        {
            try
            {
                var claims = await _storageService.ReadAllAsync<Claim>(CLAIMS_FILE);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    claims = claims.Where(c =>
                        (c.PatientName?.ToLower().Contains(searchLower) ?? false) ||
                        (c.ProviderName?.ToLower().Contains(searchLower) ?? false) ||
                        (c.ClaimNumber?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(claimStatus))
                {
                    claims = claims.Where(c => 
                        c.ClaimStatus?.Equals(claimStatus, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                return claims.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting claims count");
                throw;
            }
        }

        public async Task<int> CreateClaimAsync(CreateClaimRequest request, int userId)
        {
            try
            {
                _logger.LogInformation($"Creating new claim by user {userId}");

                var newClaim = new Claim
                {
                    ClaimNumber = request.ClaimNumber,
                    PatientName = request.PatientName,
                    ProviderName = request.ProviderName,
                    DateOfService = request.DateOfService,
                    ClaimAmount = request.ClaimAmount,
                    ClaimStatus = request.ClaimStatus ?? "Pending",
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };

                int claimId = await _storageService.AddAsync(CLAIMS_FILE, newClaim);
                _logger.LogInformation($"Claim created with ID: {claimId}");
                
                return claimId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                throw;
            }
        }

        public async Task UpdateClaimAsync(int claimId, UpdateClaimRequest request, int userId)
        {
            try
            {
                _logger.LogInformation($"Updating claim {claimId} by user {userId}");

                var claims = await _storageService.ReadAllAsync<Claim>(CLAIMS_FILE);
                var claim = claims.FirstOrDefault(c => c.ClaimId == claimId);

                if (claim == null)
                {
                    throw new KeyNotFoundException($"Claim with ID {claimId} not found");
                }

                claim.PatientName = request.PatientName;
                claim.ProviderName = request.ProviderName;
                claim.DateOfService = request.DateOfService;
                claim.ClaimAmount = request.ClaimAmount;
                claim.ClaimStatus = request.ClaimStatus;
                claim.LastModifiedBy = userId;
                claim.LastModifiedDate = DateTime.UtcNow;

                await _storageService.UpdateAsync(CLAIMS_FILE, claimId, claim);
                _logger.LogInformation($"Claim {claimId} updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating claim {claimId}");
                throw;
            }
        }

        public async Task DeleteClaimAsync(int claimId)
        {
            try
            {
                _logger.LogInformation($"Deleting claim {claimId}");
                await _storageService.DeleteAsync<Claim>(CLAIMS_FILE, claimId);
                _logger.LogInformation($"Claim {claimId} deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting claim {claimId}");
                throw;
            }
        }

        private List<Claim> ApplySorting(List<Claim> claims, string? sortBy, string? sortDirection)
        {
            var isDescending = sortDirection?.Equals("DESC", StringComparison.OrdinalIgnoreCase) ?? false;

            return (sortBy?.ToUpper()) switch
            {
                "CLAIMNUMBER" => isDescending 
                    ? claims.OrderByDescending(c => c.ClaimNumber).ToList()
                    : claims.OrderBy(c => c.ClaimNumber).ToList(),
                "PATIENTNAME" => isDescending
                    ? claims.OrderByDescending(c => c.PatientName).ToList()
                    : claims.OrderBy(c => c.PatientName).ToList(),
                "CLAIMAMOUNT" => isDescending
                    ? claims.OrderByDescending(c => c.ClaimAmount).ToList()
                    : claims.OrderBy(c => c.ClaimAmount).ToList(),
                "CLAIMSTATUS" => isDescending
                    ? claims.OrderByDescending(c => c.ClaimStatus).ToList()
                    : claims.OrderBy(c => c.ClaimStatus).ToList(),
                _ => isDescending
                    ? claims.OrderByDescending(c => c.CreatedDate).ToList()
                    : claims.OrderBy(c => c.CreatedDate).ToList()
            };
        }
    }
}
