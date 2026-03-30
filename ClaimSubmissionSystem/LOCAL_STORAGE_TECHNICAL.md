# Local Storage Implementation - Technical Deep Dive

## Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│          ASP.NET Core API Controllers                │
│    ┌──────────────────┬──────────────────┐            │
│    │  AuthController  │  ClaimsController│            │
│    └────────┬─────────┴────────┬─────────┘            │
│             │                  │                      │
├─────────────┼──────────────────┼────────────────────┤
│ Dependency Injection (DI)                            │
│    ┌────────────────────────────────────────┐        │
│    │  IAuthRepository                       │        │
│    │  IClaimsRepository                     │        │
│    └────────┬──────────────────┬────────────┘        │
│             │                  │                      │
├─────────────┼──────────────────┼────────────────────┤
│ Repository Implementations                          │
│    ┌────────────────────────────────────────┐        │
│    │  LocalAuthRepository                   │        │
│    │  LocalClaimsRepository                 │        │
│    └────────────┬──────────────┬────────────┘        │
│                 │              │                      │
├─────────────────┼──────────────┼────────────────────┤
│ Core Service - LocalStorageService                  │
│  ├─ ReadAllAsync<T>(fileName)                       │
│  ├─ ReadByIdAsync<T>(fileName, id)                  │
│  ├─ WriteAllAsync<T>(fileName, items)               │
│  ├─ AddAsync<T>(fileName, item)                     │
│  ├─ UpdateAsync<T>(fileName, id, item)              │
│  ├─ DeleteAsync<T>(fileName, id)                    │
│  └─ QueryAsync<T>(fileName, predicate)              │
│      │                                               │
│      └─ JSON File Operations                         │
│         └─ Thread-safe with locks                   │
└────────────────────────────────────────────────────┘
        ↓                                               
┌─────────────────────────────────────────────────────┐
│         Persistent JSON Files                        │
│    ┌──────────────────┬──────────────────┐            │
│    │  users.json      │  claims.json     │            │
│    └──────────────────┴──────────────────┘            │
│     (Located in: Data/LocalStorage/data/)             │
└─────────────────────────────────────────────────────┘
```

## Component Details

### 1. LocalStorageService

**Purpose:** Abstracts all file I/O operations and provides a generic interface for JSON persistence.

**Key Methods:**

#### ReadAllAsync<T>(fileName)
- Loads entire JSON file into memory
- Returns empty list if file doesn't exist
- Thread-safe file reading
- JSON deserialization with case-insensitive property matching

```csharp
public async Task<List<T>> ReadAllAsync<T>(string fileName) where T : class
{
    lock (_lockObj)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        if (!File.Exists(filePath))
            return new List<T>();
        
        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
    }
}
```

#### ReadByIdAsync<T>(fileName, id)
- Uses reflection to find ID property
- Searches by either "Id" or "{TypeName}Id" convention
- Returns null if not found

```csharp
var idProperty = typeof(T).GetProperty("Id") 
    ?? typeof(T).GetProperty($"{typeof(T).Name}Id");
return items.FirstOrDefault(item => 
    idProperty.GetValue(item) is int itemId && itemId == id);
```

#### AddAsync<T>(fileName, item)
- Auto-generates next ID (max existing ID + 1)
- Sets ID on the new item using reflection
- Appends to existing items and saves

```csharp
int newId = items.Any() 
    ? items.Max(item => (int)(idProperty.GetValue(item) ?? 0)) + 1 
    : 1;
idProperty.SetValue(item, newId);
items.Add(item);
await WriteAllAsync(fileName, items);
return newId;
```

#### UpdateAsync<T>(fileName, id, updatedItem)
- Finds existing item by ID
- Copies all properties from updated item (reflection-based)
- Saves entire list back to file

#### DeleteAsync<T>(fileName, id)
- Finds and removes item by ID
- Saves updated list to file

#### QueryAsync<T>(fileName, predicate)
- Loads all items and applies LINQ filter
- Used for complex searches with custom predicates

### 2. LocalAuthRepository

**Interface:** `IAuthRepository`

**Public Methods:**

#### ValidateCredentialsAsync(username, password)
- Queries users.json for username (case-insensitive)
- Checks if user is active
- Verifies password using BCrypt
- Returns User object if valid, null otherwise

```csharp
// Get user
var user = users.FirstOrDefault(u => 
    u.Username?.Equals(username, StringComparison.OrdinalIgnoreCase) == true 
    && u.IsActive);

if (user == null) return null;

// Verify with BCrypt
bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash ?? "");
if (!passwordMatch) return null;

return user; // Success
```

**BCrypt Details:**
- Algorithm: bcrypt with salt rounds = 11 (default)
- Hash format: `$2a$11$...` (base-64 encoded)
- Resistant to rainbow table attacks
- Modern alternative to SHA256

#### CreateUserAsync(username, passwordHash, email, fullName)
- Checks for duplicate username
- Assigns next available UserId
- Sets `IsActive = true` and `CreatedDate = NOW`
- Saves to users.json

#### UpdateLastLoginAsync(userId)
- Finds user by ID
- Sets `LastLoginDate` to UTC now
- Non-critical operation (doesn't throw on failure)

### 3. LocalClaimsRepository

**Interface:** `IClaimsRepository`

**Public Methods:**

#### GetClaimByIdAsync(claimId)
- Direct lookup by ID using `ReadByIdAsync`

#### GetClaimByNumberAsync(claimNumber)
- Iterates through all claims
- Case-insensitive string comparison

#### GetClaimsAsync(GetClaimsRequest)
Most complex method - handles filtering, sorting, and pagination:

**Step 1: Load all claims**
```csharp
var claims = await _storageService.ReadAllAsync<Claim>(CLAIMS_FILE);
```

**Step 2: Apply search filter**
```csharp
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
{
    var searchLower = request.SearchTerm.ToLower();
    claims = claims.Where(c =>
        (c.PatientName?.ToLower().Contains(searchLower) ?? false) ||
        (c.ProviderName?.ToLower().Contains(searchLower) ?? false) ||
        (c.ClaimNumber?.ToLower().Contains(searchLower) ?? false)
    ).ToList();
}
```

**Step 3: Apply status filter**
```csharp
if (!string.IsNullOrWhiteSpace(request.ClaimStatus))
{
    claims = claims.Where(c => 
        c.ClaimStatus?.Equals(request.ClaimStatus, 
            StringComparison.OrdinalIgnoreCase) == true
    ).ToList();
}
```

**Step 4: Apply sorting**
```csharp
// Switch based on SortBy field
claims = (request.SortBy?.ToUpper()) switch
{
    "CLAIMNUMBER" => isDescending 
        ? claims.OrderByDescending(c => c.ClaimNumber).ToList()
        : claims.OrderBy(c => c.ClaimNumber).ToList(),
    // ... other fields ...
    _ => isDescending
        ? claims.OrderByDescending(c => c.CreatedDate).ToList()
        : claims.OrderBy(c => c.CreatedDate).ToList()
};
```

**Step 5: Apply pagination**
```csharp
int totalRecords = claims.Count;
var paginatedClaims = claims
    .Skip((request.PageNumber - 1) * request.PageSize)
    .Take(request.PageSize)
    .Select(c => new ClaimResponse { ... })
    .ToList();

return new PaginatedClaimsResponse
{
    Claims = paginatedClaims,
    TotalRecords = totalRecords,
    PageNumber = request.PageNumber,
    PageSize = request.PageSize
    // TotalPages computed: (TotalRecords + PageSize - 1) / PageSize
};
```

#### CreateClaimAsync(request, userId)
- Auto-generates ClaimId
- Sets default status to "Pending" if not provided
- Records creator (CreatedBy = userId)
- Timestamps creation

#### UpdateClaimAsync(claimId, request, userId)
- Requires claim to exist (throws KeyNotFoundException if not)
- Updates all mutable fields
- Records modifier (LastModifiedBy = userId)
- Updates LastModifiedDate

#### DeleteClaimAsync(claimId)
- Removes claim from file
- No error if already deleted

#### GetClaimsCountAsync(searchTerm, claimStatus)
- Returns filtered count without pagination
- Used for metadata/analytics

### 4. DataSeeder

**Purpose:** Initialize the system with test data on first run.

**Process:**

1. **Service Initialization**
```csharp
var storageService = new LocalStorageService(...);
```

2. **Check Existing Users**
```csharp
var existingUsers = await storageService.ReadAllAsync<User>(usersFile);
if (existingUsers.Any(u => u.Username == "admin" || u.Username == "claimmanager"))
    return; // Already seeded
```

3. **Create Test Users**
```csharp
string adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
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
    // ... claimmanager user ...
};
await storageService.WriteAllAsync(usersFile, users);
```

4. **Create Sample Claims**
```csharp
var claims = new List<Claim>
{
    new Claim 
    { 
        ClaimId = 1,
        ClaimNumber = "CLM-2024-001",
        PatientName = "John Doe",
        // ... other fields ...
    },
    // ... more claims ...
};
await storageService.WriteAllAsync(claimsFile, claims);
```

## Thread Safety & Concurrency

### Lock Mechanism
```csharp
private static readonly object _lockObj = new();

// Protected section
lock (_lockObj)
{
    var json = File.ReadAllText(filePath);
    // ... read/deserialize ...
}
```

**Why Needed:**
- Multiple concurrent requests may access same file
- File I/O isn't atomic (read, modify, write)
- Lock ensures only one operation at a time

**Performance Impact:**
- Acceptable for small-medium concurrent users
- For high concurrency, consider:
  - In-memory caching with periodic persistence
  - Database backend
  - Message queue for operations

## DependencyInjection Registration

**In Program.cs:**
```csharp
// Single instance (reused across entire application)
builder.Services.AddSingleton<LocalStorageService>();

// New instance per request (allows concurrent safe operations)
builder.Services.AddScoped<IClaimsRepository, LocalClaimsRepository>();
builder.Services.AddScoped<IAuthRepository, LocalAuthRepository>();
```

**Why Singleton for LocalStorageService?**
- Stateless utility service
- Thread-safe with internal locks
- Reduces memory overhead

**Why Scoped for Repositories?**
- Repositories may maintain state during request
- Each request gets isolated instance
- Prevents cross-request contamination

## Error Handling

### LocalStorageService
```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    _logger.LogError(ex, $"Error reading from {fileName}");
    throw; // Always throw to propagate to controller
}
```

### LocalAuthRepository
```csharp
// BCrypt verification failure is caught, not critical
catch (Exception bcryptEx)
{
    _logger.LogWarning(bcryptEx, "BCrypt verification failed");
    return null; // Return null instead of throwing
}
```

### LocalClaimsRepository
```csharp
catch (KeyNotFoundException) 
{
    // Specific exception for "not found"
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Generic operation error");
    throw;
}
```

## Performance Characteristics

| Operation | Time Complexity | Space Complexity | Notes |
|-----------|-----------------|------------------|-------|
| ReadAllAsync | O(n) | O(n) | Loads entire file |
| ReadByIdAsync | O(n) | O(1) | Linear search |
| AddAsync | O(n) | O(n) | Full rewrite |
| UpdateAsync | O(n) | O(n) | Full rewrite |
| DeleteAsync | O(n) | O(n) | Full rewrite |
| QueryAsync | O(n) | O(m) | m = result set |

**For optimal performance:**
- Keep claims per file < 10,000
- Implement pagination (don't load all at once)
- Use filters to reduce dataset early

## Extending the System

### Adding New Entity Type

**1. Create Domain Model**
```csharp
public class Invoice
{
    public int InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    // ... other properties ...
}
```

**2. Create Repository Interface**
```csharp
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
    Task<List<Invoice>> GetAllAsync();
    Task<int> CreateAsync(Invoice invoice);
    Task UpdateAsync(int id, Invoice invoice);
    Task DeleteAsync(int id);
}
```

**3. Implement Repository**
```csharp
public class LocalInvoiceRepository : IInvoiceRepository
{
    private readonly LocalStorageService _storageService;
    private const string INVOICES_FILE = "invoices.json";
    
    public LocalInvoiceRepository(LocalStorageService storageService)
    {
        _storageService = storageService;
    }
    
    public async Task<Invoice?> GetByIdAsync(int id) =>
        await _storageService.ReadByIdAsync<Invoice>(INVOICES_FILE, id);
    
    // ... implement other methods ...
}
```

**4. Register in DI**
```csharp
builder.Services.AddScoped<IInvoiceRepository, LocalInvoiceRepository>();
```

**5. Create Controller**
```csharp
[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceRepository _repository;
    // ... implementation ...
}
```

## Migration to Database

If moving back to SQL Server:

**1. Keep Domain Models & Interfaces** (don't change)
```csharp
public class Claim { ... }
public interface IClaimsRepository { ... }
```

**2. Create New Repository Implementation**
```csharp
public class SqlClaimsRepository : IClaimsRepository
{
    // SQL-based implementation
}
```

**3. Update DI Registration**
```csharp
// Change from:
builder.Services.AddScoped<IClaimsRepository, LocalClaimsRepository>();

// To:
builder.Services.AddScoped<IClaimsRepository, SqlClaimsRepository>();
```

**Controllers don't need to change** - they depend on interface, not implementation!

## Testing Considerations

### Unit Testing LocalStorageService
```csharp
[Test]
public async Task AddAsync_GeneratesSequentialIds()
{
    var service = new LocalStorageService(mockLogger);
    var item1 = new User { Username = "user1" };
    var item2 = new User { Username = "user2" };
    
    int id1 = await service.AddAsync("test.json", item1);
    int id2 = await service.AddAsync("test.json", item2);
    
    Assert.AreEqual(1, id1);
    Assert.AreEqual(2, id2);
}
```

### Integration Testing Repositories
```csharp
[Test]
public async Task ValidateCredentialsAsync_WithValidCredentials_ReturnsUser()
{
    var appFactory = new WebApplicationFactory<Program>();
    var client = appFactory.CreateClient();
    
    var response = await client.PostAsJsonAsync(
        "/api/auth/login",
        new { username = "admin", password = "Admin@123" }
    );
    
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

## Monitoring & Logging

### Key Log Points
```csharp
_logger.LogInformation("Creating local storage data seeding...");
_logger.LogDebug($"Validating credentials for user: {username}");
_logger.LogWarning("Test users already exist, skipping seeding");
_logger.LogError(ex, "Error creating user");
```

### Log Levels
- **Information:** Major operations (startup, seeding)
- **Debug:** Detailed operation flow (validation steps)
- **Warning:** Non-critical issues (best-effort operations)
- **Error:** Critical failures (requires attention)

---

This local storage implementation provides a fully functional, self-contained data persistence layer that requires no database infrastructure while maintaining clean architecture principles.
