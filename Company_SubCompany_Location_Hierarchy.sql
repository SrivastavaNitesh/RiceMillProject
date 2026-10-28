SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.tbl_company', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_company
    (
        CompanyId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_company PRIMARY KEY,
        CompanyCode NVARCHAR(50) NOT NULL,
        CompanyName NVARCHAR(200) NOT NULL,
        OfficeId INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_company_IsActive DEFAULT (1),
        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_company_CreatedDate DEFAULT (SYSDATETIME()),
        ModifiedDate DATETIME2(0) NULL,
        CONSTRAINT UQ_tbl_company_CompanyCode UNIQUE (CompanyCode),
        CONSTRAINT FK_tbl_company_o05_office FOREIGN KEY (OfficeId) REFERENCES dbo.o05_office(OfficeId)
    );
END;

IF OBJECT_ID('dbo.tbl_subcompany', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_subcompany
    (
        SubCompanyId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_subcompany PRIMARY KEY,
        CompanyId INT NOT NULL,
        SubCompanyCode NVARCHAR(50) NOT NULL,
        SubCompanyName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_subcompany_IsActive DEFAULT (1),
        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_subcompany_CreatedDate DEFAULT (SYSDATETIME()),
        ModifiedDate DATETIME2(0) NULL,
        CONSTRAINT UQ_tbl_subcompany_CompanyCode UNIQUE (CompanyId, SubCompanyCode),
        CONSTRAINT FK_tbl_subcompany_tbl_company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_company(CompanyId)
    );
END;

IF COL_LENGTH('dbo.m_LocationMaster', 'SubCompanyId') IS NULL
    ALTER TABLE dbo.m_LocationMaster ADD SubCompanyId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_m_LocationMaster_tbl_subcompany')
    ALTER TABLE dbo.m_LocationMaster WITH CHECK ADD CONSTRAINT FK_m_LocationMaster_tbl_subcompany
        FOREIGN KEY (SubCompanyId) REFERENCES dbo.tbl_subcompany(SubCompanyId);

DECLARE @OfficeId INT =
(
    SELECT TOP (1) OfficeId
    FROM dbo.o05_office
    WHERE IsActive = 1 AND OfficeType = 1
    ORDER BY OfficeId
);

IF @OfficeId IS NULL
    THROW 50001, 'Active main office/company was not found.', 1;

DECLARE @CompanyId INT;
SELECT @CompanyId = CompanyId FROM dbo.tbl_company WHERE OfficeId = @OfficeId;

IF @CompanyId IS NULL
BEGIN
    INSERT dbo.tbl_company (CompanyCode, CompanyName, OfficeId)
    SELECT CONCAT('COMP-', @OfficeId), LTRIM(RTRIM(OfficeName)), OfficeId
    FROM dbo.o05_office
    WHERE OfficeId = @OfficeId;
    SET @CompanyId = CONVERT(INT, SCOPE_IDENTITY());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_subcompany WHERE CompanyId = @CompanyId AND SubCompanyCode = 'SHYAMA')
    INSERT dbo.tbl_subcompany (CompanyId, SubCompanyCode, SubCompanyName)
    VALUES (@CompanyId, 'SHYAMA', 'Shyama Enterprises');

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_subcompany WHERE CompanyId = @CompanyId AND SubCompanyCode = 'POOJA')
    INSERT dbo.tbl_subcompany (CompanyId, SubCompanyCode, SubCompanyName)
    VALUES (@CompanyId, 'POOJA', 'Pooja Enterprises');

-- Existing NULL SubCompanyId values are intentionally preserved because a
-- location may belong directly to the main company.

COMMIT;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ManageLocationMaster
    @Action VARCHAR(20),
    @LocationId INT = NULL,
    @LocationCode NVARCHAR(20) = NULL OUTPUT,
    @LocationName NVARCHAR(100) = NULL,
    @LocationType NVARCHAR(50) = NULL,
    @Capacity DECIMAL(18,2) = NULL,
    @CapacityUnit NVARCHAR(10) = NULL,
    @Description NVARCHAR(250) = NULL,
    @Address NVARCHAR(250) = NULL,
    @Remarks NVARCHAR(250) = NULL,
    @IsActive BIT = 1,
    @CreatedBy INT = 1,
    @OfficeId INT = NULL,
    @SubCompanyId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Action IN ('INSERT', 'UPDATE')
    BEGIN
        IF ISNULL(@OfficeId, 0) = 0 THROW 50001, 'Company selection is required.', 1;
        IF ISNULL(@SubCompanyId, 0) > 0 AND NOT EXISTS
        (
            SELECT 1
            FROM dbo.tbl_subcompany subcompany
            INNER JOIN dbo.tbl_company company ON company.CompanyId = subcompany.CompanyId
            WHERE subcompany.SubCompanyId = @SubCompanyId
              AND subcompany.IsActive = 1
              AND company.IsActive = 1
              AND company.OfficeId = @OfficeId
        )
            THROW 50003, 'Selected subcompany does not belong to the selected company.', 1;
    END;

    IF @Action = 'INSERT'
    BEGIN
        BEGIN TRANSACTION;
        DECLARE @NextId INT, @Suffix INT;
        SELECT @NextId = ISNULL(MAX(LocationId), 0) + 1 FROM dbo.m_LocationMaster WITH (UPDLOCK, HOLDLOCK);
        SELECT @Suffix = ISNULL(MAX(TRY_CONVERT(INT, SUBSTRING(LocationCode, 5, 20))), 0) + 1 FROM dbo.m_LocationMaster WITH (UPDLOCK, HOLDLOCK);
        IF @Suffix < @NextId SET @Suffix = @NextId;
        SET @LocationCode = 'LOC-' + CONVERT(VARCHAR(15), @Suffix);
        WHILE EXISTS (SELECT 1 FROM dbo.m_LocationMaster WITH (UPDLOCK, HOLDLOCK) WHERE LocationCode = @LocationCode)
        BEGIN
            SET @Suffix += 1;
            SET @LocationCode = 'LOC-' + CONVERT(VARCHAR(15), @Suffix);
        END;

        INSERT dbo.m_LocationMaster
            (LocationCode, LocationName, LocationType, Capacity, CapacityUnit, Description, Address, Remarks, IsActive, CreatedBy, CreatedDate, SubCompanyId)
        VALUES
            (@LocationCode, @LocationName, @LocationType, ISNULL(@Capacity, 0), @CapacityUnit, @Description, @Address, @Remarks, ISNULL(@IsActive, 1), @CreatedBy, GETDATE(), @SubCompanyId);

        SET @LocationId = CONVERT(INT, SCOPE_IDENTITY());
        INSERT dbo.t_OfficeLocationMapping
            (OfficeId, LocationId, UnloadingAllowed, EffectiveFrom, IsActive, CreatedBy, CreatedDate)
        VALUES
            (@OfficeId, @LocationId, 1, CONVERT(DATE, GETDATE()), 1, @CreatedBy, GETDATE());
        COMMIT;
        SELECT @LocationId AS LocationId, @LocationCode AS LocationCode;
        RETURN;
    END;

    IF @Action = 'UPDATE'
    BEGIN
        UPDATE dbo.m_LocationMaster
        SET LocationName = @LocationName, LocationType = @LocationType, Capacity = ISNULL(@Capacity, 0),
            CapacityUnit = @CapacityUnit, Description = @Description, Address = @Address, Remarks = @Remarks,
            IsActive = @IsActive, SubCompanyId = @SubCompanyId, ModifiedBy = @CreatedBy, ModifiedDate = GETDATE()
        WHERE LocationId = @LocationId;

        UPDATE dbo.t_OfficeLocationMapping
        SET IsActive = 0, ModifiedBy = @CreatedBy, ModifiedDate = GETDATE()
        WHERE LocationId = @LocationId AND OfficeId <> @OfficeId AND IsActive = 1;

        IF EXISTS (SELECT 1 FROM dbo.t_OfficeLocationMapping WHERE LocationId = @LocationId AND OfficeId = @OfficeId)
            UPDATE dbo.t_OfficeLocationMapping
            SET IsActive = 1, ModifiedBy = @CreatedBy, ModifiedDate = GETDATE()
            WHERE LocationId = @LocationId AND OfficeId = @OfficeId;
        ELSE
            INSERT dbo.t_OfficeLocationMapping
                (OfficeId, LocationId, UnloadingAllowed, EffectiveFrom, IsActive, CreatedBy, CreatedDate)
            VALUES
                (@OfficeId, @LocationId, 1, CONVERT(DATE, GETDATE()), 1, @CreatedBy, GETDATE());
        RETURN;
    END;

    IF @Action IN ('SELECT_ALL', 'SELECT_BY_ID')
    BEGIN
        SELECT location.*, mapping.OfficeId, office.OfficeName,
               company.CompanyId, subcompany.SubCompanyName, company.CompanyName
        FROM dbo.m_LocationMaster location
        OUTER APPLY
        (
            SELECT TOP (1) item.OfficeId
            FROM dbo.t_OfficeLocationMapping item
            WHERE item.LocationId = location.LocationId AND item.IsActive = 1
            ORDER BY item.MappingId DESC
        ) mapping
        LEFT JOIN dbo.o05_office office ON office.OfficeId = mapping.OfficeId
        LEFT JOIN dbo.tbl_subcompany subcompany ON subcompany.SubCompanyId = location.SubCompanyId
        LEFT JOIN dbo.tbl_company company ON company.CompanyId = subcompany.CompanyId
            OR (subcompany.SubCompanyId IS NULL AND company.OfficeId = mapping.OfficeId)
        WHERE @Action = 'SELECT_ALL' OR location.LocationId = @LocationId
        ORDER BY location.LocationId DESC;
        RETURN;
    END;

    IF @Action = 'DELETE'
    BEGIN
        UPDATE dbo.m_LocationMaster SET IsActive = 0 WHERE LocationId = @LocationId;
        UPDATE dbo.t_OfficeLocationMapping SET IsActive = 0 WHERE LocationId = @LocationId;
    END;
END;
GO

SELECT company.CompanyId, company.CompanyName, subcompany.SubCompanyId, subcompany.SubCompanyName,
       location.LocationId, location.LocationName
FROM dbo.tbl_company company
LEFT JOIN dbo.tbl_subcompany subcompany ON subcompany.CompanyId = company.CompanyId
LEFT JOIN dbo.m_LocationMaster location ON location.SubCompanyId = subcompany.SubCompanyId
ORDER BY company.CompanyId, subcompany.SubCompanyId, location.LocationId;
