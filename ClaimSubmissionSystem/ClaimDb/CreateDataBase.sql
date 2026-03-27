IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ClaimSubmissionDB')
BEGIN
    CREATE DATABASE ClaimSubmissionDB;
END
GO

USE ClaimSubmissionDB;
GO

-- Create Users Table for Authentication
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserId INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(256) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        LastLoginDate DATETIME,
        LastModifiedDate DATETIME
    );
END
GO

-- Create Claims Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Claims')
BEGIN
    CREATE TABLE Claims (
        ClaimId INT PRIMARY KEY IDENTITY(1,1),
        ClaimNumber NVARCHAR(50) NOT NULL UNIQUE,
        PatientName NVARCHAR(100) NOT NULL,
        ProviderName NVARCHAR(100) NOT NULL,
        DateOfService DATE NOT NULL,
        ClaimAmount DECIMAL(10, 2) NOT NULL,
        ClaimStatus NVARCHAR(50) NOT NULL,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        LastModifiedBy INT,
        LastModifiedDate DATETIME,
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
        FOREIGN KEY (LastModifiedBy) REFERENCES Users(UserId)
    );
END
GO

-- Create Index on ClaimNumber for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_ClaimNumber')
BEGIN
    CREATE INDEX IX_Claims_ClaimNumber ON Claims(ClaimNumber);
END
GO

-- Create Index on ClaimStatus for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_ClaimStatus')
BEGIN
    CREATE INDEX IX_Claims_ClaimStatus ON Claims(ClaimStatus);
END
GO

PRINT 'Database creation completed successfully.';
