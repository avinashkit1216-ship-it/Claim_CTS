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

        public async Task<User?> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                _logger.LogDebug($"Starting credential validation for user: {username}");

                using var connection = new SqlConnection(_connectionString);
                
                // Fetch user by username
                _logger.LogDebug($"Querying database for user: {username}");
                var user = await connection.QueryFirstOrDefaultAsync<User>(
                    "SELECT UserId, Username, Email, FullName, IsActive, PasswordHash FROM Users WHERE Username = @Username AND IsActive = 1",
                    new { Username = username }
                );

                if (user == null)
                {
                    _logger.LogWarning($"User '{username}' not found or inactive in database");
                    return null;
                }

                _logger.LogDebug($"User '{username}' found (ID: {user.UserId}). Validating password...");

                // Verify password using BCrypt (import BCrypt.Net for this)
                Boolean passwordMatch = false;
                try
                {
                    _logger.LogDebug($"Attempting BCrypt password verification for user: {username}");
                    passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash ?? "");
                    _logger.LogDebug($"BCrypt verification result: {passwordMatch}");
                }
                catch (Exception bcryptEx)
                {
                    _logger.LogWarning(bcryptEx, $"BCrypt verification failed for user '{username}', attempting SHA256 compatibility check");
                    
                    // If BCrypt verification fails, try SHA256 hash for backward compatibility
                    try
                    {
                        using var sha256 = System.Security.Cryptography.SHA256.Create();
                        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                        var hashedPassword = System.BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
                        passwordMatch = hashedPassword == user.PasswordHash;
                        _logger.LogDebug($"SHA256 verification result: {passwordMatch}");
                    }
                    catch (Exception sha256Ex)
                    {
                        _logger.LogError(sha256Ex, $"SHA256 verification also failed for user: {username}");
                        throw;
                    }
                }

                if (!passwordMatch)
                {
                    _logger.LogWarning($"Password mismatch for user '{username}'");
                    return null;
                }

                _logger.LogInformation($"Password validated successfully for user: {username}");

                return new User
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsActive = user.IsActive
                };
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, $"SQL database error during credential validation for user '{username}': {sqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error validating credentials for user '{username}': {ex.GetType().Name} - {ex.Message}");
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

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var count = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE Email = @Email",
                    new { Email = email }
                );
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if email exists: {email}");
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var count = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE Username = @Username",
                    new { Username = username }
                );
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if username exists: {username}");
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                return await connection.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Email = @Email",
                    new { Email = email }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user by email: {email}");
                throw;
            }
        }
    }
}
