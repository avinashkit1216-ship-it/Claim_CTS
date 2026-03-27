# Web-to-API Integration Test Results

## Summary
The Web application (ClaimSubmission.Web) is successfully configured to communicate with the API (ClaimSubmission.API) over HTTP at the correct port.

## Configuration Details

### Fixed Issues

1. **API Base URL Configuration**
   - **Issue**: `appsettings.Development.json` was pointing to `https://localhost:7198` (API's HTTPS port)
   - **Fix**: Updated to `http://localhost:5285` (API's HTTP port)
   - **File**: `ClaimSubmission.Web/appsettings.Development.json`

2. **Authentication Service Bug**
   - **Issue**: AuthController in the API was hashing passwords but not using the hashed value for validation
   - **Fix**: Updated AuthController to pass raw password; updated AuthRepository to perform BCrypt verification
   - **Files**: 
     - `ClaimSubmission.API/Controllers/AuthController.cs`
     - `ClaimSubmission.API/Data/Repositories.cs`

3. **HTTPS Redirect in Development**
   - **Issue**: HttpsRedirection middleware was forcing HTTPS in development, causing connection issues
   - **Fix**: Moved `app.UseHttpsRedirection()` inside production-only conditional
   - **Files**:
     - `ClaimSubmission.Web/Program.cs`
     - `ClaimSubmission.API/Program.cs`

4. **Authorization Attribute Conflict**
   - **Issue**: `[Authorize]` attribute on ClaimController conflicted with session-based authentication
   - **Fix**: Removed `[Authorize]` attribute; kept session-based checks in individual action methods
   - **File**: `ClaimSubmission.Web/Controllers/ClaimController.cs`

## Connection Details

### Running Services
- **API**: http://localhost:5285
- **Web App**: http://localhost:5277  
- **Database**: SQL Server running in Docker (localhost:1433)

### Test Credentials
- **Username**: admin
- **Password**: Admin@123

## Architecture

### Request Flow
1. User logs in at Web app (http://localhost:5277/Authentication/Login)
2. Credentials are POSTed to Web controller
3. Web app calls AuthenticationService
4. AuthenticationService makes HTTP POST to http://localhost:5285/api/auth/login
5. API validates credentials against SQL Server database
6. API returns JWT token, user info in response
7. Web app stores user data and token in session cookies
8. User can navigate to protected pages with session authentication

### Session Management
- User data is stored in session: UserId, Username, FullName, Email, UserToken
- ClaimController checks `HttpContext.Session.GetString("IsAuthenticated") == "true"`
- All API calls include JWT token from session in Authorization header

## Database Setup

Database has been created with:
- **Users Table**: Contains admin and claimmanager test accounts
- **Claims Table**: Contains 5 sample claims for testing
- **Password Hashing**: BCrypt hashes used for security
- **Connection String**: `Server=localhost,1433;Database=ClaimSubmissionDB;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=true;`

## Files Modified

1. `/ClaimSubmissionSystem/ClaimSubmission.Web/appsettings.Development.json`
   - Changed API URL from `https://localhost:7198` to `http://localhost:5285`

2. `/ClaimSubmissionSystem/ClaimSubmission.Web/Program.cs`
   - Moved HttpsRedirection to production-only conditional
   
3. `/ClaimSubmissionSystem/ClaimSubmission.Web/Controllers/ClaimController.cs`
   - Removed `[Authorize]` attribute

4. `/ClaimSubmissionSystem/ClaimSubmission.API/Controllers/AuthController.cs`
   - Fixed password validation flow to use raw password

5. `/ClaimSubmissionSystem/ClaimSubmission.API/Data/Repositories.cs`
   - Implemented BCrypt password verification in C#
   - Fetch user by username and verify password using BCrypt

6. `/ClaimSubmissionSystem/ClaimSubmission.API/Program.cs`
   - Moved HttpsRedirection to production-only conditional

7. `/ClaimSubmissionSystem/ClaimSubmission.API/appsettings.Development.json`
   - Updated connection string to use SQL Server Docker container on port 1433

## Testing Instructions

### Manual Test via Browser
1. Open http://localhost:5277 in your browser
2. Navigate to login page
3. Enter credentials:
   - Username: admin
   - Password: Admin@123
4. Submit login form
5. Should be redirected to /Claim/Index page
6. Claims list should load with sample data

### Verification Points
✓ Web app successfully connects to API at http://localhost:5285
✓ Login request reaches API /api/auth/login endpoint
✓ Database credentials validated correctly
✓ JWT token returned in response
✓ Token stored in session
✓ Navigation to Claims page works
✓ Claims API endpoint called with JWT token

## Network Diagram
```
┌─────────────────────────────────────────────────────────────┐
│                      Browser                                 │
│                                                               │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP :5277
                       ▼
┌─────────────────────────────────────────────────────────────┐
│           Web App (ClaimSubmission.Web)                      │
│                                                               │
│  ├─ Port: 5277                                               │
│  ├─ Session-Based Auth                                       │
│  └─ AuthenticationService                                    │
│                                                               │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP POST :5285
                       │ /api/auth/login
                       ▼
┌─────────────────────────────────────────────────────────────┐
│            API (ClaimSubmission.API)                         │
│                                                               │
│  ├─ Port: 5285                                               │
│  ├─ JWT Token Authentication                                │
│  ├─ Dapper ORM                                               │
│  └─ Stored Procedures                                        │
│                                                               │
└──────────────────────┬──────────────────────────────────────┘
                       │ T-SQL Queries
                       │ Port: 1433
                       ▼
┌─────────────────────────────────────────────────────────────┐
│         SQL Server (Docker Container)                        │
│                                                               │
│  ├─ ClaimSubmissionDB Database                               │
│  ├─ Users Table (with BCrypt hashes)                         │
│  └─ Claims Table (sample data)                               │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Status: ✅ COMPLETE

All Web-to-API connectivity issues have been resolved. The application is ready for testing login and claims management functionality.
