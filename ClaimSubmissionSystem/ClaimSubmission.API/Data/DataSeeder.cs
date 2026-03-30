using ClaimSubmission.API.Data.LocalStorage;
using ClaimSubmission.API.Models;

namespace ClaimSubmission.API.Data
{
    /// <summary>
    /// Utility for seeding initial data to local storage with proper password hashing
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// Seeds default test users and sample claims with local storage
        /// </summary>
        public static async Task SeedTestUsersAsync(IConfiguration configuration, ILogger logger)
        {
            try
            {
                logger.LogInformation("Starting local storage data seeding...");
                
                var storageService = new LocalStorageService(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<LocalStorageService>());
                
                // Seed test users
                await SeedTestUsersToStorageAsync(storageService, logger);
                
                // Seed sample claims
                await SeedSampleClaimsToStorageAsync(storageService, logger);
                
                logger.LogInformation("Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during data seeding");
                throw;
            }
        }

        private static async Task SeedTestUsersToStorageAsync(LocalStorageService storageService, ILogger logger)
        {
            try
            {
                const string testPassword = "Admin@123";
                const string usersFile = "users.json";

                // Read existing users
                var existingUsers = await storageService.ReadAllAsync<User>(usersFile);
                
                if (existingUsers.Any(u => u.Username == "admin" || u.Username == "claimmanager"))
                {
                    logger.LogInformation("Test users already exist, skipping user seeding");
                    return;
                }

                logger.LogInformation("Creating test users...");

                // Generate BCrypt hashes for test password
                string adminHash = BCrypt.Net.BCrypt.HashPassword(testPassword);
                string managerHash = BCrypt.Net.BCrypt.HashPassword(testPassword);

                var users = new List<User>
                {
                    new User
                    {
                        UserId = 1,
                        Username = "admin",
                        PasswordHash = adminHash,
                        Email = "admin@claimsystem.com",
                        FullName = "Administrator",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new User
                    {
                        UserId = 2,
                        Username = "claimmanager",
                        PasswordHash = managerHash,
                        Email = "manager@claimsystem.com",
                        FullName = "John Manager",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    }
                };

                await storageService.WriteAllAsync(usersFile, users);
                logger.LogInformation("Created test users");
                logger.LogInformation($"Test credentials: Username='admin', Password='{testPassword}'");
                logger.LogInformation($"Test credentials: Username='claimmanager', Password='{testPassword}'");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding test users");
                throw;
            }
        }

        private static async Task SeedSampleClaimsToStorageAsync(LocalStorageService storageService, ILogger logger)
        {
            try
            {
                const string claimsFile = "claims.json";

                // Read existing claims
                var existingClaims = await storageService.ReadAllAsync<Claim>(claimsFile);
                
                if (existingClaims.Any())
                {
                    logger.LogInformation("Sample claims already exist, skipping claims seeding");
                    return;
                }

                logger.LogInformation("Creating sample claims...");

                var claims = new List<Claim>
                {
                    new Claim
                    {
                        ClaimId = 1,
                        ClaimNumber = "CLM-2024-001",
                        PatientName = "John Doe",
                        ProviderName = "Smith Medical Clinic",
                        DateOfService = DateTime.UtcNow.AddDays(-10),
                        ClaimAmount = 1500.00m,
                        ClaimStatus = "Approved",
                        CreatedBy = 1,
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new Claim
                    {
                        ClaimId = 2,
                        ClaimNumber = "CLM-2024-002",
                        PatientName = "Jane Smith",
                        ProviderName = "Central Hospital",
                        DateOfService = DateTime.UtcNow.AddDays(-5),
                        ClaimAmount = 3500.00m,
                        ClaimStatus = "Pending",
                        CreatedBy = 1,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new Claim
                    {
                        ClaimId = 3,
                        ClaimNumber = "CLM-2024-003",
                        PatientName = "Michael Johnson",
                        ProviderName = "Wellness Clinic",
                        DateOfService = DateTime.UtcNow.AddDays(-2),
                        ClaimAmount = 750.00m,
                        ClaimStatus = "Pending",
                        CreatedBy = 2,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    }
                };

                await storageService.WriteAllAsync(claimsFile, claims);
                logger.LogInformation("Created sample claims");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding sample claims");
                throw;
            }
        }
    }
}
