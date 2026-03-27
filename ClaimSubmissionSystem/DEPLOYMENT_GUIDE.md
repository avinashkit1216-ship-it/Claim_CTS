# ClaimSubmission System - Deployment & Testing Guide

## System Overview
- **Web Project**: `ClaimSubmission.Web` - ASP.NET Core MVC running on port 5277 (HTTP) / 7277 (HTTPS)
- **API Project**: `ClaimSubmission.API` - ASP.NET Core REST API running on port 5285 (HTTP) / 7285 (HTTPS)
- **Database**: SQL Server - Database name: `ClaimDb`

## Prerequisites

### Required Software
- .NET 10.0 SDK
- SQL Server (local or remote)
- Recommended: SQL Server Management Studio for database verification

### Database Setup
1. Ensure SQL Server is running
2. Run the database creation scripts in order:
   ```bash
   # From ClaimSubmissionSystem/ClaimDb directory
   - CreateDataBase.sql
   - Table_Claims.sql
   - AuthenticationStoredProcedures.sql
   - ClaimStoredProcedures.sql
   - SampleData.sql
   ```

## Configuration

### Development Environment

#### API Configuration (ClaimSubmission.API)
- **Port**: 5285 (HTTP), 7285 (HTTPS)
- **launchSettings Profile**: `http` or `https`
- **Connection String**: 
  ```
  Server=localhost;Database=ClaimDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;
  ```
- **Test Credentials** (auto-seeded):
  - Username: `admin` / Password: `Admin@123`
  - Username: `claimmanager` / Password: `Admin@123`

#### Web Configuration (ClaimSubmission.Web)
- **Port**: 5277 (HTTP), 7277 (HTTPS)
- **launchSettings Profile**: `http` or `https`
- **API Base URL**: http://localhost:5285 (from appsettings.json)
- **Session Timeout**: 20 minutes
- **HttpClient Timeout**: 30 seconds

### Production Environment

#### API Configuration (appsettings.Production.json)
- Update connection string with production SQL Server details
- Change JWT Secret Key (minimum 32 characters)
- Use HTTPS only (https://api.example.com)
- Encrypt=True for encrypted connections

#### Web Configuration (appsettings.Production.json)
- Update ApiBaseUrl to production API endpoint (https://api.example.com)
- Update AllowedHosts for CORS
- Use HTTPS only

## Running the Application

### Development Mode

#### Terminal 1 - Start API
```bash
cd ClaimSubmissionSystem/ClaimSubmission.API
dotnet run --launch-profile http
# or
dotnet run --launch-profile https
```

#### Terminal 2 - Start Web
```bash
cd ClaimSubmissionSystem/ClaimSubmission.Web
dotnet run --launch-profile http
# or
dotnet run --launch-profile https
```

### Verify Services Are Running
- **API Health Check**: `http://localhost:5285/health`
- **API Swagger UI**: `http://localhost:5285/swagger`
- **Web Application**: `http://localhost:5277` → should redirect to login

## Testing Workflow

### Test 1: API Startup & Port Configuration
```bash
Expected:
✓ API starts on port 5285 without "Address already in use" error
✓ CORS headers configured for http://localhost:5277
✓ /health endpoint returns {"status":"healthy","timestamp":"..."}
✓ /swagger is accessible and shows endpoints
```

### Test 2: Database Connectivity & Seeding
```bash
Check API console logs:
✓ "Initializing database seeding..."
✓ "Created 'admin' user"
✓ "Created 'claimmanager' user"
✓ "Data seeding completed successfully"

If seeding fails:
- Verify SQL Server is running
- Check connection string in appsettings.json
- Run database scripts manually
```

### Test 3: Login Workflow
```bash
Steps:
1. Navigate to http://localhost:5277 (redirects to /Authentication/Login)
2. Enter username: admin, password: Admin@123
3. Click Submit

Expected:
✓ Successful login message
✓ Redirects to Claims Index page
✓ Session stores: UserId, Username, FullName, Email, UserToken, IsAuthenticated
✓ Browser shows authenticated state (can check in Session in controller)
```

### Test 4: Claims List with Pagination
```bash
Steps:
1. Navigate to http://localhost:5277/Claim/Index
2. Observe first page of claims
3. Try different page numbers: ?pageNumber=2, ?pageNumber=3
4. Try page size: ?pageSize=10, ?pageSize=50

Expected:
✓ Claims load without errors
✓ Pagination calculations work (no DivideByZeroException)
✓ Page numbers increment/decrement correctly
✓ Total pages calculated correctly
✓ Token from session is used for authorization
```

### Test 5: Navigation Across Pages
```bash
Steps:
1. Login as admin
2. Navigate to Claims list
3. Click navigation links (if available)
4. Go back to Home
5. Go back to Claims

Expected:
✓ Token persists across all pages (session-based)
✓ No re-authentication required
✓ User remains logged in during session timeout period
✓ Page navigation is smooth
```

### Test 6: Error Handling

#### Invalid Login Credentials
```bash
Steps:
1. Login.cshtml page
2. Enter username: admin, password: wrongpassword
3. Click Submit

Expected:
✓ Message: "Invalid username or password"
✓ Returns to login page
✓ No 500 error in browser or API logs
```

#### API Service Unavailable
```bash
Steps:
1. Stop the API service
2. Try to login from Web application

Expected:
✓ Web shows: "Authentication service is currently unavailable. Please try again later."
✓ Does NOT crash with 500 error
✓ API logs show connection attempt
✓ Graceful degradation
```

#### Invalid Pagination Parameters
```bash
Steps:
1. Try ?pageNumber=0&pageSize=0
2. Try ?pageNumber=-1&pageSize=-1

Expected:
✓ API returns 400 BadRequest: "PageNumber and PageSize must be greater than 0"
✓ Web handles gracefully with default values (pageNumber=1, pageSize=20)
```

### Test 7: Security - Session Cookie
```bash
Steps:
1. Login as admin
2. Open Browser DevTools → Application → Cookies
3. Find cookie: "ASPXCORE_SESSIONID"
4. Verify cookie properties:
   - HttpOnly: true
   - Secure: true (in HTTPS)
   - SameSite: Lax (default)

Expected:
✓ Session cookie exists
✓ HttpOnly flag prevents JavaScript access
✓ Token safely stored in session, not exposed to client-side code
```

## Troubleshooting

### Issue: "Address already in use" on port 5285 or 5277
```bash
Solution:
# Find process on port 5285
netstat -ano | findstr :5285

# Kill process (Windows)
taskkill /PID <PID> /F

# On Linux/Mac
lsof -i :5285
kill -9 <PID>
```

### Issue: "Cannot open database 'ClaimDb'"
```bash
Solution:
1. Verify SQL Server is running
2. Check connection string in appsettings.json
3. Run CreateDataBase.sql script
4. Verify database exists: SELECT * FROM sys.databases;
```

### Issue: "The certificate chain was issued by an authority that is not trusted"
```bash
Solution (Development only):
- Use HTTP instead of HTTPS during testing
- Or use self-signed certificate with TrustServerCertificate=true
```

### Issue: Login fails with 503 Service Unavailable
```bash
Solution:
1. Check if API is running: http://localhost:5285/health
2. Verify database connection
3. Check API logs for SQL errors
4. Restart both services
```

### Issue: Claims page shows empty even though data exists
```bash
Solution:
1. Verify token is in session (check in ClaimController)
2. Check if API returns 401 Unauthorized (authorization issue)
3. Verify claims exist in database: SELECT * FROM Claims;
4. Check API logs for errors
```

## Production Deployment Checklist

- [ ] Update appsettings.Production.json with production values
- [ ] Change JWT signing key to cryptographically secure random value (32+ characters)
- [ ] Update connection string to production SQL Server
- [ ] Update AllowedHosts to production domain
- [ ] Configure HTTPS with valid SSL certificate
- [ ] Enable logging to persistent storage (file, Azure Application Insights, etc.)
- [ ] Set up monitoring and alerting
- [ ] Test all authentication flows with production configuration
- [ ] Run security scan for vulnerabilities
- [ ] Load test to ensure scalability
- [ ] Set up backup and disaster recovery
- [ ] Document production procedures

## Key Improvements Implemented

1. ✅ **Port Configuration**: No conflicts, clean startup
2. ✅ **Database Connectivity**: Proper connection strings with timeouts
3. ✅ **Error Handling**: Appropriate HTTP status codes (400, 401, 503, 500)
4. ✅ **Pagination Safety**: Guards against DivideByZeroException
5. ✅ **Session Management**: Token stored securely in session cookie
6. ✅ **Logging**: Comprehensive debug logging in Development, minimal in Production
7. ✅ **API Documentation**: Swagger/OpenAPI available at /swagger
8. ✅ **CORS Configuration**: Properly configured for Web-to-API communication

## Support & Debugging

### Enable Debug Logging
```json
// appsettings.Development.json
"Logging": {
  "LogLevel": {
    "Default": "Debug",  // Change to Debug for verbose output
    "Microsoft.AspNetCore": "Information"
  }
}
```

### Monitor API Health
```bash
# Health endpoint
curl http://localhost:5285/health

# Swagger API documentation
# Browser: http://localhost:5285/swagger
```

### Database Verification
```sql
-- Check if tables exist
SELECT * FROM sys.tables WHERE name IN ('Users', 'Claims');

-- Count users
SELECT COUNT(*) as UserCount FROM Users;

-- Count claims
SELECT COUNT(*) as ClaimCount FROM Claims;
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with username/password, returns token

### Claims Management
- `GET /api/claims?pageNumber=1&pageSize=20` - List claims with pagination
- `GET /api/claims/{id}` - Get specific claim
- `POST /api/claims` - Create new claim
- `PUT /api/claims/{id}` - Update claim
- `DELETE /api/claims/{id}` - Delete claim

### Health
- `GET /health` - API health status
- `GET /` - API information

For full API documentation, visit: `http://localhost:5285/swagger`
