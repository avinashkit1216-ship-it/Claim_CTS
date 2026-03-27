# Database Setup Guide for Claim Submission System

## Prerequisites

- SQL Server 2019 or later (or SQL Server Express)
- SQL Server Management Studio (SSMS) - Optional but recommended
- .NET 10.0 SDK

## Setup Steps

### Option 1: Using SQL Server Management Studio (GUI)

#### Step 1: Create Database
1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Right-click on "Databases" → "New Database..."
4. Name: `ClaimSubmissionDB`
5. Click "OK"

#### Step 2: Create Tables
1. Right-click on the new database → "New Query"
2. Copy and paste contents from `01_Database/Table_Claims.sql`
3. Execute (F5)
4. Repeat for any other table scripts in the `01_Database/` folder

#### Step 3: Create Stored Procedures
1. Right-click on the database → "New Query"
2. Copy and paste contents from `01_Database/AuthenticationStoredProcedures.sql`
3. Execute (F5)
4. Repeat for `01_Database/ClaimStoredProcedures.sql`

#### Step 4: Insert Sample Data
1. Right-click on the database → "New Query"
2. Copy and paste contents from `01_Database/SampleData.sql`
3. Execute (F5)

### Option 2: Using Command Line (sqlcmd)

```bash
# Navigate to the database folder
cd ClaimSubmissionSystem/01_Database

# Create database and tables
sqlcmd -S localhost -U sa -P YourPassword -i CreateDataBase.sql
sqlcmd -S localhost -U sa -P YourPassword -i Table_Claims.sql

# Create stored procedures
sqlcmd -S localhost -U sa -P YourPassword -i AuthenticationStoredProcedures.sql
sqlcmd -S localhost -U sa -P YourPassword -i ClaimStoredProcedures.sql

# Insert sample data
sqlcmd -S localhost -U sa -P YourPassword -i SampleData.sql
```

### Option 3: Using Docker (Recommended for Development)

#### Start SQL Server in Docker:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrongPassword123!" \
  -p 1433:1433 --name sql-server \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

#### Execute setup scripts:
```bash
# Wait for SQL Server to be ready (30-60 seconds)

# Create database and tables
docker exec -it sql-server /opt/mssql-tools/bin/sqlcmd -S localhost \
  -U sa -P YourStrongPassword123! \
  -i /var/opt/mssql/CreateDataBase.sql
```

## Verify Database Setup

### Check if database exists:
```sql
SELECT name FROM sys.databases WHERE name = 'ClaimSubmissionDB'
```

### Check if tables exist:
```sql
USE ClaimSubmissionDB
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'
```

Expected tables:
- Users
- Claims

### Check if stored procedures exist:
```sql
USE ClaimSubmissionDB
SELECT name FROM sys.objects WHERE type = 'P'
```

Expected procedures:
- sp_User_ValidateCredentials
- sp_User_Create
- sp_User_UpdateLastLogin
- sp_Claim_CreateClaimAsync
- sp_Claim_GetAllWithFiltering
- (and others)

### Check if sample data exists:
```sql
USE ClaimSubmissionDB
SELECT * FROM Users
```

Should return at least:
- Username: `admin` (will be created by DataSeeder)
- Username: `claimmanager` (will be created by DataSeeder)

## Connection String Configuration

The connection string is defined in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ClaimSubmissionDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### For Different Scenarios:

**Local Named Instance:**
```
Server=.\SQLEXPRESS;Database=ClaimSubmissionDB;Trusted_Connection=true;TrustServerCertificate=true;
```

**Remote Server with SQL Authentication:**
```
Server=your-server.database.windows.net;Database=ClaimSubmissionDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
```

**Docker Container:**
```
Server=localhost,1433;Database=ClaimSubmissionDB;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=true;
```

## Troubleshooting

### "Connection string not found"
- Ensure `appsettings.json` exists in `ClaimSubmission.API/`
- Check that `DefaultConnection` is properly configured

### "Could not open connection to SQL Server"
- Verify SQL Server is running
- Check server name and port
- Ensure database exists
- Verify login credentials

### "Login failed for user"
- Check if using correct authentication mode (Windows vs SQL)
- Verify user has permissions on the database
- Check if database is marked as offline

### "Cannot find stored procedure"
- Verify stored procedures were created successfully
- Check `sp_help` to list all procedures: `EXEC sp_help 'sp_User_ValidateCredentials'`
- Re-run the stored procedure creation scripts

### "Users table not found"
- Check if database was created: `SELECT DB_ID('ClaimSubmissionDB')`
- List all tables: `SELECT * FROM INFORMATION_SCHEMA.TABLES`
- Re-run the table creation scripts

## Running the Application

Once the database is set up:

### Terminal 1: Start the API
```bash
cd ClaimSubmissionSystem/ClaimSubmission.API
dotnet run
# API will be available at http://localhost:5285
```

The API will automatically seed test users on first run (in Development environment).

### Terminal 2: Start the Web App
```bash
cd ClaimSubmissionSystem/ClaimSubmission.Web
dotnet run
# Web app will be available at http://localhost:5277 or https://localhost:7205
```

### Test the Login Flow
```bash
# Method 1: Using the test script
cd ClaimSubmissionSystem
bash test_login_api.sh

# Method 2: Using curl
curl -X POST http://localhost:5285/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'

# Method 3: Using the Web UI
Navigate to http://localhost:5277/Authentication/Login (or https://localhost:7205)
```

## Resetting the Database

If you need to reset and start fresh:

```bash
# Using SQL Server Management Studio
# 1. Right-click on database → Delete
# 2. Confirm deletion
# 3. Re-run all setup steps

# Or using sqlcmd
sqlcmd -S localhost -U sa -P YourPassword -Q "DROP DATABASE ClaimSubmissionDB"
# Then re-run all setup scripts
```

## Database Maintenance

### Backup Database
```sql
BACKUP DATABASE ClaimSubmissionDB 
TO DISK = 'C:\Backups\ClaimSubmissionDB.bak'
WITH FORMAT, INIT, NAME = 'ClaimSubmissionDB Backup'
```

### Restore Database
```sql
RESTORE DATABASE ClaimSubmissionDB
FROM DISK = 'C:\Backups\ClaimSubmissionDB.bak'
WITH REPLACE
```

### Clear All Data (Keep Schema)
```sql
USE ClaimSubmissionDB
TRUNCATE TABLE Claims
TRUNCATE TABLE Users
```

## Security Notes

⚠️ **Development Only:**
- Default test credentials are: `admin` / `Admin@123`
- Connection strings use `Trusted_Connection=true` (Windows auth)
- Never use these credentials in production

🔒 **Production Considerations:**
- Use strong passwords
- Enable SQL Server encryption
- Use certificates for connection strings
- Implement proper access controls
- Enable audit logging
- Use SQL Server Always Encrypted for sensitive data
