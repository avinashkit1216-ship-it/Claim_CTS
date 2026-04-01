# Implementation Guide: Production-Ready ASP.NET Core MVC Project

## 🎯 Complete Step-by-Step Fix & Optimization Plan

### PART 1: SECURITY FIXES

---

## 1.1. Remove Hardcoded Credentials & Use User Secrets

### Step 1: Update appsettings.json (Remove Secrets)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=ClaimSubmissionDB;Integrated Security=true;"
  },
  "Jwt": {
    "Issuer": "ClaimSubmissionAPI",
    "Audience": "ClaimSubmissionClients",
    "ExpirationMinutes": 60
  },
  "AllowedHosts": "*",
  "CORS": {
    "AllowedOrigins": "http://localhost:5277,https://localhost:7277"
  }
}
```

### Step 2: Create appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ClaimSubmissionDB;Integrated Security=true;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "your-development-secret-key-that-is-at-least-32-characters"
  },
  "CORS": {
    "AllowedOrigins": "http://localhost:5277,http://localhost:3000"
  }
}
```

### Step 3: Create appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "CORS": {
    "AllowedOrigins": "https://yourdomain.com"
  }
}
```

---

## 1.2. Fix Critical Issue: Extract UserId from JWT Claims

### File: ClaimSubmission.API/Controllers/ClaimsController.cs

**Replace this:**
```csharp
public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequest request)
{
    // ...validation...
    
    // Extract userId from claims (placeholder - normally from JWT token)
    var userId = 1; // TODO: Get real user ID from claims
    
    var claimId = await _repository.CreateClaimAsync(request, userId);
```

**With this:**
```csharp
public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequest request)
{
    try
    {
        if (request == null)
        {
            return BadRequest(new { error = "Claim data is required" });
        }

        var validationErrors = ValidateCreateClaimRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new { errors = validationErrors });
        }

        // FIXED: Extract userId from JWT claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim?.Value, out var userId) || userId <= 0)
        {
            _logger.LogWarning("Invalid or missing user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token" });
        }

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
```

---

## 1.3. Add Authorization & HTTPS Enforcement

### File: ClaimSubmission.API/Program.cs

**Replace:**
```csharp
app.UseRouting();
app.UseCors("AllowWeb");
app.UseAuthorization();
```

**With:**
```csharp
// HTTPS enforcement
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts(); // HTTP Strict-Transport-Security
}

app.UseRouting();
app.UseCors("AllowWeb");
app.UseAuthentication();
app.UseAuthorization();
```

---

## 1.4. Add JWT Authentication & Authorization

### File: ClaimSubmission.API/Program.cs

**Add after CORS setup:**
```csharp
// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClaimSubmissionAPI";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ClaimSubmissionClients";

    if (string.IsNullOrEmpty(jwtKey))
    {
        throw new InvalidOperationException("JWT Key must be configured in appsettings");
    }

    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();
```

**Update CORS policy:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policyBuilder =>
    {
        var allowedOrigins = builder.Configuration["CORS:AllowedOrigins"]?.Split(',') 
            ?? new[] { "http://localhost:5277", "https://localhost:7277" };
        
        policyBuilder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Authorization attribute to controllers
[Authorize]
public class ClaimsController : ControllerBase
{
    // ...
}
```

---

## PART 2: DATABASE OPTIMIZATION

---

## 2.1. Add Comprehensive Database Indexes

### File: ClaimDb/CreateDataBase.sql

**Add after table creation:**
```sql
-- Indexes on Users table
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email')
BEGIN
    CREATE INDEX IX_Users_Email ON Users(Email);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username')
BEGIN
    CREATE INDEX IX_Users_Username ON Users(Username);
END
GO

-- Indexes on Claims table for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_CreatedBy')
BEGIN
    CREATE INDEX IX_Claims_CreatedBy ON Claims(CreatedBy);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_DateOfService')
BEGIN
    CREATE INDEX IX_Claims_DateOfService ON Claims(DateOfService);
END
GO

-- Composite index for common queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_Status_Date')
BEGIN
    CREATE INDEX IX_Claims_Status_Date ON Claims(ClaimStatus, CreatedDate DESC);
END
GO

-- Index for search optimization
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_PatientName')
BEGIN
    CREATE INDEX IX_Claims_PatientName ON Claims(PatientName);
END
GO
```

---

## 2.2. Add Audit Logging Table

### File: ClaimDb/CreateDataBase.sql

**Add:**
```sql
-- Create Audit Log table for HIPAA compliance
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        AuditLogId INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId INT,
        OldValue NVARCHAR(MAX),
        NewValue NVARCHAR(MAX),
        IPAddress NVARCHAR(50),
        UserAgent NVARCHAR(MAX),
        CreatedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END
GO

-- Index for audit queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_UserId_Date')
BEGIN
    CREATE INDEX IX_AuditLogs_UserId_Date ON AuditLogs(UserId, CreatedDate DESC);
END
GO

-- Add soft delete columns to Claims
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Claims' AND COLUMN_NAME = 'IsDeleted')
BEGIN
    ALTER TABLE Claims ADD IsDeleted BIT NOT NULL DEFAULT 0;
    ALTER TABLE Claims ADD DeletedDate DATETIME NULL;
    CREATE INDEX IX_Claims_IsDeleted ON Claims(IsDeleted);
END
GO
```

---

## PART 3: ARCHITECTURE & CODE QUALITY

---

## 3.1. Create Constants File

### File: ClaimSubmission.API/Constants/AppConstants.cs

```csharp
namespace ClaimSubmission.API.Constants
{
    /// <summary>
    /// Application-wide constants for healthcare claims management
    /// </summary>
    public static class AppConstants
    {
        // Claim Status Constants
        public static class ClaimStatus
        {
            public const string Submitted = "Submitted";
            public const string UnderReview = "Under Review";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string OnHold = "On Hold";
            public const string Paid = "Paid";

            public static readonly string[] ValidStatuses = 
            { 
                Submitted, UnderReview, Approved, Rejected, OnHold, Paid 
            };
        }

        // Session Management (Web Project)
        public static class SessionKeys
        {
            public const string UserId = "UserId";
            public const string Username = "Username";
            public const string FullName = "FullName";
            public const string Email = "Email";
            public const string UserToken = "UserToken";
            public const string IsAuthenticated = "IsAuthenticated";
        }

        // API Routes
        public static class ApiRoutes
        {
            public const string AuthLogin = "api/auth/login";
            public const string AuthRegister = "api/auth/register";
            public const string ClaimsBase = "api/claims";
            public const string HealthCheck = "health";
        }

        // Defaults
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int DefaultTimeoutSeconds = 30;
        public const int JwtExpirationMinutes = 60;

        // Data Retention (Years)
        public const int ClaimRetentionYears = 7; // HIPAA requires 6+ years
        public const int AuditLogRetentionYears = 5;
    }

    /// <summary>
    /// Error message constants
    /// </summary>
    public static class ErrorMessages
    {
        public const string InvalidCredentials = "Invalid username or password";
        public const string UserNotFound = "User not found";
        public const string ClaimNotFound = "Claim not found";
        public const string UnauthorizedAccess = "You do not have permission to perform this action";
        public const string ServiceUnavailable = "Service is temporarily unavailable";
        public const string InternalError = "An internal error occurred. Please try again later.";
    }

    /// <summary>
    /// Regular expression patterns
    /// </summary>
    public static class RegexPatterns
    {
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const string StrongPassword = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).{8,}$";
        public const string PhoneNumber = @"^\d{3}-\d{3}-\d{4}$";
        public const string ClaimNumber = @"^CLM-\d{6}$";
    }
}
```

---

## 3.2. Create DTO Validators

### File: ClaimSubmission.API/Validators/LoginRequestValidator.cs

```csharp
using System.ComponentModel.DataAnnotations;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Constants;

namespace ClaimSubmission.API.Validators
{
    public static class DtoValidators
    {
        public static List<string> ValidateLoginRequest(LoginRequest request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Login request cannot be null");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(request.Username))
                errors.Add("Username is required");
            else if (request.Username.Length > 100)
                errors.Add("Username cannot exceed 100 characters");

            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required");
            else if (request.Password.Length < 8)
                errors.Add("Password must be at least 8 characters");

            return errors;
        }

        public static List<string> ValidateRegisterRequest(RegisterRequest request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Register request cannot be null");
                return errors;
            }

            // Email validation
            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email is required");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, AppConstants.RegexPatterns.Email))
                errors.Add("Invalid email format");

            // Full Name validation
            if (string.IsNullOrWhiteSpace(request.FullName))
                errors.Add("Full name is required");
            else if (request.FullName.Length > 100)
                errors.Add("Full name cannot exceed 100 characters");

            // Password validation
            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required");
            else if (request.Password.Length < 8)
                errors.Add("Password must be at least 8 characters");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Password, AppConstants.RegexPatterns.StrongPassword))
                errors.Add("Password must contain uppercase, lowercase, numbers, and special characters");

            // Confirm password
            if (request.Password != request.ConfirmPassword)
                errors.Add("Passwords do not match");

            return errors;
        }

        public static List<string> ValidateCreateClaimRequest(CreateClaimRequest request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Claim request cannot be null");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(request.PatientName))
                errors.Add("Patient name is required");
            else if (request.PatientName.Length > 100)
                errors.Add("Patient name cannot exceed 100 characters");

            if (string.IsNullOrWhiteSpace(request.ProviderName))
                errors.Add("Provider name is required");
            else if (request.ProviderName.Length > 100)
                errors.Add("Provider name cannot exceed 100 characters");

            if (request.DateOfService == default(DateTime))
                errors.Add("Date of service is required");
            else if (request.DateOfService > DateTime.Now)
                errors.Add("Date of service cannot be in the future");

            if (request.ClaimAmount <= 0)
                errors.Add("Claim amount must be greater than 0");
            else if (request.ClaimAmount > 999999.99m)
                errors.Add("Claim amount cannot exceed $999,999.99");

            if (string.IsNullOrWhiteSpace(request.ClaimStatus))
                errors.Add("Claim status is required");
            else if (!AppConstants.ClaimStatus.ValidStatuses.Contains(request.ClaimStatus))
                errors.Add($"Invalid claim status. Must be one of: {string.Join(", ", AppConstants.ClaimStatus.ValidStatuses)}");

            return errors;
        }
    }
}
```

---

## PART 4: CACHING & PERFORMANCE

---

## 4.1. Add Distributed Caching Configuration

### File: ClaimSubmission.API/Program.cs

**Add:**
```csharp
// Add caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") 
        ?? "localhost:6379";
    options.Configuration = connectionString;
});

builder.Services.AddMemoryCache(); // Fallback for development
```

### File: appsettings.Development.json

**Add:**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

---

## 4.2. Create Caching Service

### File: ClaimSubmission.API/Services/CachingService.cs

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ClaimSubmission.API.Services
{
    public interface ICachingService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    }

    public class CachingService : ICachingService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CachingService> _logger;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);

        public CachingService(IDistributedCache cache, ILogger<CachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var data = await _cache.GetStringAsync(key);
                return data != null ? JsonSerializer.Deserialize<T>(data) : default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
                };

                var data = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, data, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
            }
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
                return cachedValue;

            var value = await factory();
            if (value != null)
                await SetAsync(key, value, expiration);

            return value;
        }
    }
}
```

---

## PART 5: TESTING

---

## 5.1. Create Unit Test Project

### Command:
```bash
dotnet new xunit -n ClaimSubmission.API.Tests
cd ClaimSubmission.API.Tests
dotnet add reference ../ClaimSubmission.API/ClaimSubmission.API.csproj
dotnet add package Moq
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Fluent Assertions
```

---

## 5.2. Create Core Unit Tests

### File: ClaimSubmission.API.Tests/Services/PasswordHashServiceTests.cs

```csharp
using Xunit;
using FluentAssertions;
using ClaimSubmission.API.Services;

namespace ClaimSubmission.API.Tests.Services
{
    public class PasswordHashServiceTests
    {
        private readonly PasswordHashService _service = new();

        [Fact]
        public void HashPassword_ShouldNotReturnPlaintext()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _service.HashPassword(password);

            // Assert
            hash.Should().NotBe(password);
            hash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _service.HashPassword(password);

            // Act
            var result = _service.VerifyPassword(password, hash);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword123!";
            var hash = _service.HashPassword(password);

            // Act
            var result = _service.VerifyPassword(wrongPassword, hash);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HashPassword_DifferentCalls_ShouldProduceDifferentHashes()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = _service.HashPassword(password);
            var hash2 = _service.HashPassword(password);

            // Assert
            hash1.Should().NotBe(hash2);
        }
    }
}
```

### File: ClaimSubmission.API.Tests/Validators/DtoValidatorsTests.cs

```csharp
using Xunit;
using FluentAssertions;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Validators;

namespace ClaimSubmission.API.Tests.Validators
{
    public class DtoValidatorsTests
    {
        [Fact]
        public void ValidateLoginRequest_InvalidEmail_ShouldReturnError()
        {
            // Arrange
            var request = new LoginRequest { Username = "", Password = "password" };

            // Act
            var errors = DtoValidators.ValidateLoginRequest(request);

            // Assert
            errors.Should().Contain(e => e.Contains("Username"));
        }

        [Fact]
        public void ValidateLoginRequest_ValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "TestPass123!" };

            // Act
            var errors = DtoValidators.ValidateLoginRequest(request);

            // Assert
            errors.Should().BeEmpty();
        }
    }
}
```

---

## 5.3. Create Performance Test Project

### Command:
```bash
dotnet new console -n ClaimSubmission.PerformanceTests
cd ClaimSubmission.PerformanceTests
dotnet add package BenchmarkDotNet
dotnet add package System.Net.Http
```

### File: ClaimSubmission.PerformanceTests/ClaimApiPerformanceTests.cs

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Diagnostics;

namespace ClaimSubmission.PerformanceTests
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5)]
    public class ClaimApiPerformanceTests
    {
        private HttpClient _httpClient;
        private readonly string _apiBaseUrl = "http://localhost:5285";

        [GlobalSetup]
        public void Setup()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        [Benchmark]
        public async Task RetrieveTenThousandRecords()
        {
            var pageSize = 100;
            var totalToFetch = 10000;
            var pagesRequired = (totalToFetch + pageSize - 1) / pageSize;

            var watch = Stopwatch.StartNew();

            for (int page = 1; page <= pagesRequired; page++)
            {
                var response = await _httpClient.GetAsync($"/api/claims?pageNumber={page}&pageSize={pageSize}");
                response.EnsureSuccessStatusCode();
            }

            watch.Stop();

            if (watch.Elapsed.TotalSeconds > 10)
            {
                throw new Exception($"Performance threshold exceeded: {watch.Elapsed.TotalSeconds}s");
            }
        }

        [Benchmark]
        public async Task GetSingleClaim()
        {
            var response = await _httpClient.GetAsync("/api/claims/1");
            response.EnsureSuccessStatusCode();
        }

        [Benchmark]
        public async Task SearchClaims()
        {
            var response = await _httpClient.GetAsync("/api/claims?searchTerm=Johnson&pageSize=50");
            response.EnsureSuccessStatusCode();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ClaimApiPerformanceTests>();
        }
    }
}
```

---

## 5.4. Create Integration Tests

### File: ClaimSubmission.API.Tests/Integration/AuthControllerIntegrationTests.cs

```csharp
using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using ClaimSubmission.API.DTOs;

namespace ClaimSubmission.API.Tests.Integration
{
    public class AuthControllerIntegrationTests : IAsyncLifetime
    {
        private HttpClient _httpClient;
        private WebApplicationFactory<Program> _factory;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>();
            _httpClient = _factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
            _factory?.Dispose();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var loginRequest = new LoginRequest 
            { 
                Username = "testuser", 
                Password = "TestPassword123!" 
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsAsync<dynamic>();
            content.data.token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest 
            { 
                Username = "wronguser", 
                Password = "wrongpassword" 
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
```

---

## PART 6: REFACTORING & ORGANIZATION

---

## 6.1. Remove Code Duplication

### File: ClaimSubmission.Web/Controllers/ClaimController.cs

**Replace both Index() and List() methods with consolidated version:**

```csharp
[HttpGet]
[HttpGet("{action}")]
[Authorize]
public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20, 
    string? searchTerm = null, string? claimStatus = null, 
    string? sortBy = "CreatedDate", string? sortDirection = "DESC")
{
    try
    {
        if (!IsUserAuthenticated())
            return RedirectToAction("Login", "Authentication");

        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || pageSize > AppConstants.MaxPageSize) 
            pageSize = AppConstants.DefaultPageSize;

        string token = GetUserToken();
        
        var claims = await _claimApiService.GetClaimsAsync(token, pageNumber, pageSize, 
            searchTerm, claimStatus, sortBy, sortDirection);

        if (claims == null)
        {
            claims = new ClaimsPaginatedListViewModel 
            { 
                Claims = new List<ClaimViewListModel>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = 0
            };
        }

        return View("Index", claims);
    }
    catch (UnauthorizedAccessException)
    {
        _logger.LogWarning("Unauthorized access attempt");
        return RedirectToAction("Login", "Authentication");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading claims");
        return View("Error", new { message = ErrorMessages.InternalError });
    }
}

// Alias for backward compatibility
public async Task<IActionResult> List(int pageNumber = 1, int pageSize = 20, 
    string? searchTerm = null, string? claimStatus = null)
{
    return await Index(pageNumber, pageSize, searchTerm, claimStatus);
}
```

---

## 6.2. Create Web Service Base Class

### File: ClaimSubmission.Web/Services/ApiServiceBase.cs

```csharp
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClaimSubmission.Web.Services
{
    public abstract class ApiServiceBase
    {
        protected readonly HttpClient _httpClient;
        protected readonly string _apiBaseUrl;
        protected readonly ILogger _logger;

        protected ApiServiceBase(HttpClient httpClient, string apiBaseUrl, ILogger logger)
        {
            _httpClient = httpClient;
            _apiBaseUrl = apiBaseUrl;
            _logger = logger;
        }

        protected async Task<T?> GetAsync<T>(string endpoint, string? token = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/{endpoint}");
                AddAuthorizationHeader(request, token);

                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in GET request to {Endpoint}", endpoint);
                throw;
            }
        }

        protected async Task<T?> PostAsync<T>(string endpoint, object? data = null, string? token = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/{endpoint}");
                AddAuthorizationHeader(request, token);

                if (data != null)
                {
                    var json = JsonSerializer.Serialize(data);
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                }

                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in POST request to {Endpoint}", endpoint);
                throw;
            }
        }

        protected void AddAuthorizationHeader(HttpRequestMessage request, string? token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        protected string BuildQueryString(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        }
    }
}
```

---

## PART 7: MIDDLEWARE & CROSS-CUTTING CONCERNS

---

## 7.1. Create Structured Logging Configuration

### File: ClaimSubmission.API/Program.cs (Add Serilog)

```csharp
// Install: dotnet add package Serilog Serilog.AspNetCore Serilog.Sinks.File

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ClaimSubmissionAPI")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .WriteTo.File(
        "logs/claim-submission-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
);

// ... rest of configuration
```

### File: appsettings.json (Add)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 7.2. Create Audit Logging Middleware

### File: ClaimSubmission.API/Middleware/AuditLoggingMiddleware.cs

```csharp
namespace ClaimSubmission.API.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            var method = request.Method;
            var path = request.Path;

            _logger.LogInformation("Request: {Method} {Path} by User {UserId}", method, path, userId);

            await _next(context);

            var response = context.Response;
            _logger.LogInformation("Response: {Method} {Path} Status {StatusCode}", method, path, response.StatusCode);
        }
    }
}
```

---

## 7.3. Add to Program.cs

```csharp
app.UseMiddleware<AuditLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
```

---

## Complete Implementation Checklist

- [x] Security fixes (credentials, JWT, HTTPS)
- [x] Database optimization (indexes, audit logging)
- [x] Architecture improvements (DI, logging, middleware)
- [x] Code quality (constants, validators, formatters)
- [x] Caching strategy (distributed cache)
- [x] Unit testing framework
- [x] Performance testing framework
- [x] Integration testing framework
- [x] Refactoring (remove duplication)
- [x] Documentation (this guide)

