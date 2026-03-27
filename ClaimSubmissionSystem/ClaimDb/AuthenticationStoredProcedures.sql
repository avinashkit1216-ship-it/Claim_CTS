USE ClaimSubmissionDB;
GO

IF OBJECT_ID('sp_User_ValidateCredentials', 'P') IS NOT NULL
    DROP PROCEDURE sp_User_ValidateCredentials;
GO

CREATE PROCEDURE sp_User_ValidateCredentials
    @Username NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @UserId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @UserId = UserId
    FROM Users
    WHERE Username = @Username
      AND PasswordHash = @PasswordHash
      AND IsActive = 1;

    IF @UserId IS NULL
    BEGIN
        SET @UserId = -1;
    END
END
GO

-- Procedure: sp_User_Create
-- Purpose: Create a new user account
IF OBJECT_ID('sp_User_Create', 'P') IS NOT NULL
    DROP PROCEDURE sp_User_Create;
GO

CREATE PROCEDURE sp_User_Create
    @Username NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @Email NVARCHAR(100),
    @FullName NVARCHAR(100),
    @UserId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO Users (Username, PasswordHash, Email, FullName, IsActive)
        VALUES (@Username, @PasswordHash, @Email, @FullName, 1);

        SET @UserId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH
        SET @UserId = -1;
        THROW;
    END CATCH
END
GO

-- Procedure: sp_User_UpdateLastLogin
-- Purpose: Update user's last login timestamp
IF OBJECT_ID('sp_User_UpdateLastLogin', 'P') IS NOT NULL
    DROP PROCEDURE sp_User_UpdateLastLogin;
GO

CREATE PROCEDURE sp_User_UpdateLastLogin
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Users
    SET LastLoginDate = GETUTCDATE()
    WHERE UserId = @UserId;
END
GO

PRINT 'User authentication stored procedures created successfully.';
