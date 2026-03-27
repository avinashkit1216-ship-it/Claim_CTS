using System.Data;
using System.Data.SqlClient;
using Dapper;
using ClaimSubmission.API.Services;

namespace ClaimSubmission.API.Data
{
    /// <summary>
    /// Utility for seeding initial data with proper password hashing
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// Seeds default test users with BCrypt hashed passwords
        /// </summary>
        public static async Task SeedTestUsersAsync(IConfiguration configuration, ILogger logger)
        {
            try
            {
                logger.LogInformation("Starting data seeding...");
                
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    logger.LogWarning("Connection string not found for data seeding");
                    return;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                logger.LogInformation("Database connection established");

                // Check if users already exist
                var existingUsers = await connection.QueryAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE Username IN ('admin', 'claimmanager')"
                );

                if (existingUsers.FirstOrDefault() > 0)
                {
                    logger.LogInformation("Test users already exist, skipping seeding");
                    return;
                }

                // Generate BCrypt hashes for password "Admin@123"
                const string testPassword = "Admin@123";
                string adminHash = BCrypt.Net.BCrypt.HashPassword(testPassword);
                string managerHash = BCrypt.Net.BCrypt.HashPassword(testPassword);

                logger.LogInformation($"Generated BCrypt hash for test password");

                // Insert test users
                var parameters = new DynamicParameters();
                parameters.Add("@Username", "admin");
                parameters.Add("@PasswordHash", adminHash);
                parameters.Add("@Email", "admin@claimsystem.com");
                parameters.Add("@FullName", "Administrator");

                await connection.ExecuteAsync(
                    "sp_User_Create",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                logger.LogInformation("Created 'admin' user");

                parameters = new DynamicParameters();
                parameters.Add("@Username", "claimmanager");
                parameters.Add("@PasswordHash", managerHash);
                parameters.Add("@Email", "manager@claimsystem.com");
                parameters.Add("@FullName", "John Manager");

                await connection.ExecuteAsync(
                    "sp_User_Create",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                logger.LogInformation("Created 'claimmanager' user");

                logger.LogInformation("Data seeding completed successfully");
                logger.LogInformation($"Test credentials: Username='admin', Password='{testPassword}'");
                logger.LogInformation($"Test credentials: Username='claimmanager', Password='{testPassword}'");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during data seeding");
                throw;
            }
        }
    }
}
