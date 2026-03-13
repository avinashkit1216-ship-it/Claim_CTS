USE ClaimSubmissionDB;
GO


IF OBJECT_ID('sp_Claim_Create', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_Create;
GO

CREATE PROCEDURE sp_Claim_Create
    @ClaimNumber NVARCHAR(50),
    @PatientName NVARCHAR(100),
    @ProviderName NVARCHAR(100),
    @DateOfService DATE,
    @ClaimAmount DECIMAL(10, 2),
    @ClaimStatus NVARCHAR(50),
    @CreatedBy INT,
    @ClaimId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO Claims (ClaimNumber, PatientName, ProviderName, DateOfService, ClaimAmount, ClaimStatus, CreatedBy)
        VALUES (@ClaimNumber, @PatientName, @ProviderName, @DateOfService, @ClaimAmount, @ClaimStatus, @CreatedBy);

        SET @ClaimId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH
        SET @ClaimId = -1;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('sp_Claim_GetById', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_GetById;
GO

CREATE PROCEDURE sp_Claim_GetById
    @ClaimId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ClaimId,
           ClaimNumber,
           PatientName,
           ProviderName,
           DateOfService,
           ClaimAmount,
           ClaimStatus,
           CreatedBy,
           CreatedDate,
           LastModifiedBy,
           LastModifiedDate
    FROM Claims
    WHERE ClaimId = @ClaimId;
END
GO

IF OBJECT_ID('sp_Claim_GetByClaimNumber', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_GetByClaimNumber;
GO

CREATE PROCEDURE sp_Claim_GetByClaimNumber
    @ClaimNumber NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ClaimId,
           ClaimNumber,
           PatientName,
           ProviderName,
           DateOfService,
           ClaimAmount,
           ClaimStatus,
           CreatedBy,
           CreatedDate,
           LastModifiedBy,
           LastModifiedDate
    FROM Claims
    WHERE ClaimNumber = @ClaimNumber;
END
GO

-- Procedure: sp_Claim_GetAll
-- Purpose: Retrieve all claims with pagination
IF OBJECT_ID('sp_Claim_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_GetAll;
GO

CREATE PROCEDURE sp_Claim_GetAll
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SkipRows INT = (@PageNumber - 1) * @PageSize;

    SELECT ClaimId,
           ClaimNumber,
           PatientName,
           ProviderName,
           DateOfService,
           ClaimAmount,
           ClaimStatus,
           CreatedBy,
           CreatedDate,
           LastModifiedBy,
           LastModifiedDate
    FROM Claims
    ORDER BY CreatedDate DESC
    OFFSET @SkipRows ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Procedure: sp_Claim_Update
-- Purpose: Update existing claim details
IF OBJECT_ID('sp_Claim_Update', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_Update;
GO

CREATE PROCEDURE sp_Claim_Update
    @ClaimId INT,
    @PatientName NVARCHAR(100),
    @ProviderName NVARCHAR(100),
    @DateOfService DATE,
    @ClaimAmount DECIMAL(10, 2),
    @ClaimStatus NVARCHAR(50),
    @LastModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        UPDATE Claims
        SET PatientName = @PatientName,
            ProviderName = @ProviderName,
            DateOfService = @DateOfService,
            ClaimAmount = @ClaimAmount,
            ClaimStatus = @ClaimStatus,
            LastModifiedBy = @LastModifiedBy,
            LastModifiedDate = GETUTCDATE()
        WHERE ClaimId = @ClaimId;

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50001, 'Claim not found', 1;
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- Procedure: sp_Claim_Delete
-- Purpose: Delete a claim by ClaimId
IF OBJECT_ID('sp_Claim_Delete', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_Delete;
GO

CREATE PROCEDURE sp_Claim_Delete
    @ClaimId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM Claims
        WHERE ClaimId = @ClaimId;

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50001, 'Claim not found', 1;
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- Procedure: sp_Claim_GetCount
-- Purpose: Get total count of claims
IF OBJECT_ID('sp_Claim_GetCount', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_GetCount;
GO

CREATE PROCEDURE sp_Claim_GetCount
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS TotalClaims
    FROM Claims;
END
GO

PRINT 'Claim management stored procedures created successfully.';
