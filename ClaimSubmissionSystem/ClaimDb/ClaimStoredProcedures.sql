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

-- Procedure: sp_Claim_GetAllWithFiltering
-- Purpose: Retrieve claims with pagination, filtering, and sorting
IF OBJECT_ID('sp_Claim_GetAllWithFiltering', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_GetAllWithFiltering;
GO

CREATE PROCEDURE sp_Claim_GetAllWithFiltering
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @SearchTerm NVARCHAR(100) = NULL,
    @ClaimStatus NVARCHAR(50) = NULL,
    @SortBy NVARCHAR(50) = 'CreatedDate',
    @SortDirection NVARCHAR(4) = 'DESC'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SkipRows INT = (@PageNumber - 1) * @PageSize;
    DECLARE @ValidSortBy NVARCHAR(50) = CASE 
        WHEN @SortBy IN ('ClaimNumber', 'PatientName', 'ProviderName', 'DateOfService', 'ClaimAmount', 'ClaimStatus', 'CreatedDate')
        THEN @SortBy
        ELSE 'CreatedDate'
    END;
    DECLARE @ValidSortDirection NVARCHAR(4) = CASE 
        WHEN @SortDirection IN ('ASC', 'DESC')
        THEN @SortDirection
        ELSE 'DESC'
    END;

    -- Build dynamic ORDER BY clause
    DECLARE @OrderBy NVARCHAR(100) = @ValidSortBy + ' ' + @ValidSortDirection;

    -- Main query with filtering
    SELECT ClaimId,
           ClaimNumber,
           PatientName,
           ProviderName,
           DateOfService,
           ClaimAmount,
           ClaimStatus,
           CreatedDate
    FROM Claims
    WHERE (
        (@SearchTerm IS NULL) OR
        (
            ClaimNumber LIKE '%' + @SearchTerm + '%' OR
            PatientName LIKE '%' + @SearchTerm + '%' OR
            ProviderName LIKE '%' + @SearchTerm + '%'
        )
    )
    AND (
        (@ClaimStatus IS NULL) OR (ClaimStatus = @ClaimStatus)
    )
    ORDER BY CASE WHEN @ValidSortDirection = 'ASC' THEN 
        CASE @ValidSortBy
            WHEN 'ClaimNumber' THEN ClaimNumber
            WHEN 'PatientName' THEN PatientName
            WHEN 'ProviderName' THEN ProviderName
            WHEN 'DateOfService' THEN CONVERT(NVARCHAR(100), DateOfService)
            WHEN 'ClaimAmount' THEN CONVERT(NVARCHAR(100), ClaimAmount)
            WHEN 'ClaimStatus' THEN ClaimStatus
            ELSE CONVERT(NVARCHAR(100), CreatedDate)
        END
    END ASC,
    CASE WHEN @ValidSortDirection = 'DESC' THEN 
        CASE @ValidSortBy
            WHEN 'ClaimNumber' THEN ClaimNumber
            WHEN 'PatientName' THEN PatientName
            WHEN 'ProviderName' THEN ProviderName
            WHEN 'DateOfService' THEN CONVERT(NVARCHAR(100), DateOfService)
            WHEN 'ClaimAmount' THEN CONVERT(NVARCHAR(100), ClaimAmount)
            WHEN 'ClaimStatus' THEN ClaimStatus
            ELSE CONVERT(NVARCHAR(100), CreatedDate)
        END
    END DESC
    OFFSET @SkipRows ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Procedure: sp_Claim_GetCountWithFiltering
-- Purpose: Get total count of claims matching filter criteria
IF OBJECT_ID('sp_Claim_GetCountWithFiltering', 'P') IS NOT NULL
    DROP PROCEDURE sp_Claim_GetCountWithFiltering;
GO

CREATE PROCEDURE sp_Claim_GetCountWithFiltering
    @SearchTerm NVARCHAR(100) = NULL,
    @ClaimStatus NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS TotalClaims
    FROM Claims
    WHERE (
        (@SearchTerm IS NULL) OR
        (
            ClaimNumber LIKE '%' + @SearchTerm + '%' OR
            PatientName LIKE '%' + @SearchTerm + '%' OR
            ProviderName LIKE '%' + @SearchTerm + '%'
        )
    )
    AND (
        (@ClaimStatus IS NULL) OR (ClaimStatus = @ClaimStatus)
    );
END
GO

PRINT 'Claims stored procedures created successfully.';
GO

PRINT 'Claim management stored procedures created successfully.';
