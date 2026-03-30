# Local Storage Migration Guide

## Overview

The ClaimSubmissionSystem project has been successfully migrated from SQL Server to **local storage** using JSON files. This eliminates all database dependencies and makes the application fully self-contained and portable.

## What Changed

### 1. **Database Layer Removed**
- ✅ SQL Server dependency removed
- ✅ Dapper ORM connection logic removed
- ✅ SQL stored procedures replaced with in-memory operations
- ✅ No database setup scripts required

### 2. **Local Storage Implementation**
The new architecture uses JSON files stored locally:

```
ClaimSubmission.API/
  └── Data/
      └── LocalStorage/
          ├── LocalStorageService.cs      (Core JSON file operations)
          ├── LocalAuthRepository.cs      (User authentication)
          ├── LocalClaimsRepository.cs    (Claim management)
          └── data/                       (Runtime data folder)
              ├── users.json              (Auto-created on first run)
              └── claims.json             (Auto-created on first run)
```

### 3. **API Endpoints (Unchanged)**
All existing API endpoints remain the same:

|Method|Endpoint|Purpose|
|------|--------|--------|
|POST|`/api/auth/login`|User authentication|
|GET|`/api/claims`|List claims with pagination|
|GET|`/api/claims/{id}`|Get specific claim|
|POST|`/api/claims`|Create new claim|
|PUT|`/api/claims/{id}`|Update claim|
|DELETE|`/api/claims/{id}`|Delete claim|

## Running the Application

### Prerequisites
- .NET 10.0 SDK
- VS Code or Visual Studio (optional)

### Quick Start

```bash
cd ClaimSubmissionSystem/ClaimSubmission.API
dotnet build
dotnet run
```

The API will start at: `https://localhost:7272` or `http://localhost:5272`

### Testing the API

1. **Health Check:**
   ```bash
   curl https://localhost:7272/health
   ```

2. **Login (Test Credentials):**
   ```bash
   curl -X POST https://localhost:7272/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"username":"admin","password":"Admin@123"}'
   ```

3. **Get Claims:**
   ```bash
   curl https://localhost:7272/api/claims
   ```

### Swagger UI
Access interactive API documentation at: `https://localhost:7272/swagger`

## Initial Data Seeding

On first run, the application automatically creates:

### Test Users
- **Username:** `admin` | **Password:** `Admin@123`
- **Username:** `claimmanager` | **Password:** `Admin@123`

### Sample Claims
Three sample claims are created automatically for testing (visible after login)

All data is stored in JSON files in the `Data/LocalStorage/data/` directory.

## Data Storage Details

### User Data (users.json)
```json
[
  {
    "UserId": 1,
    "Username": "admin",
    "PasswordHash": "$2a$11$...",
    "Email": "admin@claimsystem.com",
    "FullName": "Administrator",
    "IsActive": true,
    "CreatedDate": "2024-01-01T00:00:00Z",
    "LastLoginDate": null
  }
]
```

### Claims Data (claims.json)
```json
[
  {
    "ClaimId": 1,
    "ClaimNumber": "CLM-2024-001",
    "PatientName": "John Doe",
    "ProviderName": "Smith Medical Clinic",
    "DateOfService": "2024-01-15T00:00:00Z",
    "ClaimAmount": 1500.00,
    "ClaimStatus": "Approved",
    "CreatedBy": 1,
    "CreatedDate": "2024-01-16T00:00:00Z",
    "LastModifiedBy": null,
    "LastModifiedDate": null
  }
]
```

## Architecture Overview

### LocalStorageService
Core service handling all file I/O operations:
- **ReadAllAsync** - Load all items from JSON file
- **ReadByIdAsync** - Get single item by ID
- **WriteAllAsync** - Save items to file
- **AddAsync** - Add new item with auto-generated ID
- **UpdateAsync** - Modify existing item
- **DeleteAsync** - Remove item
- **QueryAsync** - Search/filter items

**Key Features:**
- Thread-safe file operations using locks
- Automatic ID generation
- JSON serialization with property name case insensitivity
- Formatted JSON output for readability

### LocalAuthRepository
Handles user authentication:
- **ValidateCredentialsAsync** - Authenticate user with BCrypt password verification
- **CreateUserAsync** - Register new user
- **UpdateLastLoginAsync** - Track login history

### LocalClaimsRepository
Manages claim CRUD operations:
- **GetClaimByIdAsync** - Retrieve single claim
- **GetClaimByNumberAsync** - Find claim by claim number
- **GetClaimsAsync** - Get paginated, filtered, sorted claims
- **CreateClaimAsync** - Insert new claim
- **UpdateClaimAsync** - Modify claim
- **DeleteClaimAsync** - Remove claim

**Supported Filters:**
- Search term (searches patient name, provider name, claim number)
- Claim status (Pending, Approved, Rejected)
- Sorting (by claim number, patient name, amount, status, creation date)
- Pagination (customizable page size)

## Security

### Password Hashing
- Uses **BCrypt.Net** for secure password hashing
- Never stores plain text passwords
- Passwords hashed with salt

### JWT Authentication
- Tokens generated upon successful login
- Include user claims and expiration
- HTTPS-only in production (configured via CORS)

## File Locations

```
ClaimSubmissionSystem/
├── ClaimSubmission.API/
│   ├── Controllers/
│   │   ├── AuthController.cs        (Login endpoint)
│   │   └── ClaimsController.cs      (Claim CRUD endpoints)
│   ├── Data/
│   │   ├── LocalStorage/
│   │   │   ├── LocalStorageService.cs
│   │   │   ├── LocalAuthRepository.cs
│   │   │   ├── LocalClaimsRepository.cs
│   │   │   └── data/                (JSON files created at runtime)
│   │   ├── Repositories.cs          (Legacy - can be removed)
│   │   ├── IRepositories.cs         (Interfaces - shared with implementations)
│   │   └── DataSeeder.cs            (Initialization logic)
│   ├── Models/
│   │   └── DomainModels.cs          (User, Claim domain models)
│   ├── DTOs/
│   │   └── ClaimDto.cs              (Data Transfer Objects)
│   ├── Services/
│   │   ├── JwtTokenService.cs       (Token generation)
│   │   └── PasswordHashService.cs   (Password utilities)
│   └── Program.cs                   (Dependency injection setup)
└── ClaimSubmission.Web/             (MVC web frontend)
```

## Dependency Injection Setup

In `Program.cs`:
```csharp
// Local storage service
builder.Services.AddSingleton<LocalStorageService>();

// Local storage implementations
builder.Services.AddScoped<IClaimsRepository, LocalClaimsRepository>();
builder.Services.AddScoped<IAuthRepository, LocalAuthRepository>();

// Token and security services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
```

## Data Persistence

### How It Works
1. On startup, `DataSeeder.cs` checks if seed data exists
2. If not, it creates `users.json` and `claims.json` with initial test data
3. All changes (create, update, delete) are immediately written to JSON files
4. On application restart, all data is reloaded from files

### Thread Safety
- File operations are protected with `lock` statements
- Multiple concurrent requests are properly serialized
- Safe for multi-threaded scenarios

### Performance Considerations
- JSON files are kept in memory when accessed
- Full file rewrite on each modification (suitable for small-medium datasets)
- For large data volumes, consider migration to database or caching layer

## Migration from SQL Server

If you previously used SQL Server, here's what was removed:

### Removed Dependencies
- `System.Data.SqlClient` NuGet package
- `Dapper` ORM
- SQL Server connection strings
- Stored procedures (functionality replaced with LINQ queries)

### Removed Files/Code
- Database setup scripts (no longer needed)
- SQL connection management code
- SqlException error handling (replaced with generic exceptions)
- Stored procedure calls

### Updated Files
- `Program.cs` - Changed DI registration to use local storage
- `DataSeeder.cs` - Changed from SQL insertion to JSON file writing
- `AuthController.cs` - Removed SQL-specific error handling
- `ClaimsController.cs` - Removed SQL-specific error handling

## Troubleshooting

### Issue: "Storage directory not found"
**Solution:** The directory is auto-created. Check write permissions for the application directory.

### Issue: "JSON deserialization error"
**Solution:** Delete corrupt JSON files. New ones will be created on next startup with seed data.

### Issue: "User not found during login"
**Solution:** Verify users.json exists in `Data/LocalStorage/data/`. Restart application to reseed.

### Issue: "Claims not showing after login"
**Solution:** Check claims.json exists. The sample data loads only if the file is empty on startup.

## Environment-Specific Configuration

### Development
- Automatic data seeding enabled
- Sample data created on startup
- Detailed logging enabled
- Swagger UI available

### Production
- Same local storage mechanism
- Consider pre-seeding production data
- Implement backup strategy for JSON files
- Monitor file system access patterns

## Backup and Recovery

### Backing Up Data
```bash
# Backup both JSON files
cp -r Data/LocalStorage/data Data/LocalStorage/data.backup
```

### Recovery
```bash
# Restore from backup
cp -r Data/LocalStorage/data.backup/* Data/LocalStorage/data/
```

## Next Steps

### Optional Improvements
1. **Add data export feature** - Export claims to CSV/Excel
2. **Implement file encryption** - Encrypt JSON files at rest
3. **Add change tracking** - Log all data modifications
4. **Implement versioning** - Keep historical versions of data
5. **Add offline capability** - Make web frontend work offline
6. **Database migration** - Easy to migrate back to SQL Server if needed

### For Web Application
The `ClaimSubmission.Web` project works seamlessly with the new local storage API:

```bash
cd ClaimSubmission.Web
dotnet run
```

Navigate to `https://localhost:7277` or `http://localhost:5277`

## Support & Questions

- API Health Check: `GET /health`
- Swagger Documentation: `/swagger`
- OpenAPI Specification: `/openapi/v1.json`

## Summary

✅ **Fully functional without SQL Server**  
✅ **All data stored in JSON files**  
✅ **Automatic data seeding**  
✅ **Complete CRUD operations**  
✅ **Secure password storage with BCrypt**  
✅ **JWT-based authentication**  
✅ **Pagination and filtering support**  
✅ **Thread-safe file operations**  
✅ **Easy to backup and restore**  

The application is now ready for deployment without any database infrastructure!
