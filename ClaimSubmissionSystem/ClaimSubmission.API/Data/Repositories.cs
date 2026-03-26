using Dapper;
using System.Data;
using System.Data.SqlClient;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Models;

namespace ClaimSubmission.API.Data
{
    /// <summary>
    /// Claims repository implementation using Dapper
    /// </summary>
    public class ClaimsRepository : IClaimsRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ClaimsRepository> _logger;

        public ClaimsRepository(IConfiguration configuration, ILogger<ClaimsRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task<Claim?> GetClaimByIdAsync(int claimId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                return await connection.QueryFirstOrDefaultAsync<Claim>(
                    "sp_Claim_GetById",
                    new { ClaimId = claimId },
                    commandType: CommandType.StoredProcedure
                );
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
                using var connection = new SqlConnection(_connectionString);
                return await connection.QueryFirstOrDefaultAsync<Claim>(
                    "sp_Claim_GetByClaimNumber",
                    new { ClaimNumber = claimNumber },
                    commandType: CommandType.StoredProcedure
                );
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
                using var connection = new SqlConnection(_connectionString);
                
                // Get paginated claims
                var claims = await connection.QueryAsync<ClaimResponse>(
                    "sp_Claim_GetAllWithFiltering",
                    new
                    {
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize,
                        SearchTerm = request.SearchTerm,
                        ClaimStatus = request.ClaimStatus,
                        SortBy = request.SortBy,
                        SortDirection = request.SortDirection
                    },
                    commandType: CommandType.StoredProcedure
                );

                // Get total count
                var totalRecords = await connection.QueryFirstOrDefaultAsync<int>(
                    "sp_Claim_GetCountWithFiltering",
                    new
                    {
                        SearchTerm = request.SearchTerm,
                        ClaimStatus = request.ClaimStatus
                    },
                    commandType: CommandType.StoredProcedure
                );

                return new PaginatedClaimsResponse
                {
                    Claims = claims.ToList(),
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
                using var connection = new SqlConnection(_connectionString);
                return await connection.QueryFirstOrDefaultAsync<int>(
                    "sp_Claim_GetCountWithFiltering",
                    new { SearchTerm = searchTerm, ClaimStatus = claimStatus },
                    commandType: CommandType.StoredProcedure
                );
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
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@ClaimNumber", request.ClaimNumber);
                parameters.Add("@PatientName", request.PatientName);
                parameters.Add("@ProviderName", request.ProviderName);
                parameters.Add("@DateOfService", request.DateOfService);
                parameters.Add("@ClaimAmount", request.ClaimAmount);
                parameters.Add("@ClaimStatus", request.ClaimStatus);
                parameters.Add("@CreatedBy", userId);
                parameters.Add("@ClaimId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_Claim_Create",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return parameters.Get<int>("@ClaimId");
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
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    "sp_Claim_Update",
                    new
                    {
                        ClaimId = claimId,
                        PatientName = request.PatientName,
                        ProviderName = request.ProviderName,
                        DateOfService = request.DateOfService,
                        ClaimAmount = request.ClaimAmount,
                        ClaimStatus = request.ClaimStatus,
                        LastModifiedBy = userId
                    },
                    commandType: CommandType.StoredProcedure
                );
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
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    "sp_Claim_Delete",
                    new { ClaimId = claimId },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting claim {claimId}");
                throw;
            }
        }
    }

    /// <summary>
    /// Authentication repository implementation using Dapper
    /// </summary>
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(IConfiguration configuration, ILogger<AuthRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task<User?> ValidateCredentialsAsync(string username, string passwordHash)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var userIdParam = new DynamicParameters();
                userIdParam.Add("@Username", username);
                userIdParam.Add("@PasswordHash", passwordHash);
                userIdParam.Add("@UserId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_User_ValidateCredentials",
                    userIdParam,
                    commandType: CommandType.StoredProcedure
                );

                var userId = userIdParam.Get<int>("@UserId");
                if (userId <= 0) return null;

                // Fetch user details
                return await connection.QueryFirstOrDefaultAsync<User>(
                    "SELECT UserId, Username, Email, FullName, IsActive FROM Users WHERE UserId = @UserId",
                    new { UserId = userId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating credentials for user {username}");
                throw;
            }
        }

        public async Task<int> CreateUserAsync(string username, string passwordHash, string email, string fullName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Username", username);
                parameters.Add("@PasswordHash", passwordHash);
                parameters.Add("@Email", email);
                parameters.Add("@FullName", fullName);
                parameters.Add("@UserId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_User_Create",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return parameters.Get<int>("@UserId");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user {username}");
                throw;
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    "sp_User_UpdateLastLogin",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating last login for user {userId}");
                throw;
            }
        }
    }
}
