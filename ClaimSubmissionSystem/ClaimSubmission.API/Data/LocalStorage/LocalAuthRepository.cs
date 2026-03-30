using ClaimSubmission.API.Models;

namespace ClaimSubmission.API.Data.LocalStorage
{
    /// <summary>
    /// Authentication repository implementation using local storage (JSON files)
    /// </summary>
    public class LocalAuthRepository : IAuthRepository
    {
        private readonly LocalStorageService _storageService;
        private readonly ILogger<LocalAuthRepository> _logger;
        private const string USERS_FILE = "users.json";

        public LocalAuthRepository(LocalStorageService storageService, ILogger<LocalAuthRepository> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<User?> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                _logger.LogDebug($"Starting credential validation for user: {username}");

                // Fetch all users
                var users = await _storageService.ReadAllAsync<User>(USERS_FILE);
                
                // Find user by username
                var user = users.FirstOrDefault(u => 
                    u.Username?.Equals(username, StringComparison.OrdinalIgnoreCase) == true && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning($"User '{username}' not found or inactive");
                    return null;
                }

                _logger.LogDebug($"User '{username}' found (ID: {user.UserId}). Validating password...");

                // Verify password using BCrypt
                bool passwordMatch = false;
                try
                {
                    _logger.LogDebug($"Attempting BCrypt password verification for user: {username}");
                    passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash ?? "");
                    _logger.LogDebug($"BCrypt verification result: {passwordMatch}");
                }
                catch (Exception bcryptEx)
                {
                    _logger.LogWarning(bcryptEx, $"BCrypt verification failed for user '{username}'");
                    return null;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating credentials for user '{username}': {ex.Message}");
                throw;
            }
        }

        public async Task<int> CreateUserAsync(string username, string passwordHash, string email, string fullName)
        {
            try
            {
                _logger.LogInformation($"Creating new user: {username}");

                // Check if user already exists
                var users = await _storageService.ReadAllAsync<User>(USERS_FILE);
                if (users.Any(u => u.Username?.Equals(username, StringComparison.OrdinalIgnoreCase) == true))
                {
                    throw new InvalidOperationException($"User with username '{username}' already exists");
                }

                var newUser = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    Email = email,
                    FullName = fullName,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var userId = await _storageService.AddAsync(USERS_FILE, newUser);
                _logger.LogInformation($"User '{username}' created with ID: {userId}");
                
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user '{username}': {ex.Message}");
                throw;
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                _logger.LogDebug($"Updating last login for user ID: {userId}");

                var users = await _storageService.ReadAllAsync<User>(USERS_FILE);
                var user = users.FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for last login update");
                    return;
                }

                user.LastLoginDate = DateTime.UtcNow;
                await _storageService.UpdateAsync(USERS_FILE, userId, user);
                
                _logger.LogDebug($"Updated last login for user ID: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error updating last login for user {userId}: {ex.Message}");
                // Don't throw - this is not critical
            }
        }
    }
}
