SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @CompanyId INT =
(
    SELECT TOP (1) CompanyId
    FROM dbo.tbl_company
    WHERE IsActive = 1
    ORDER BY CompanyId
);

IF @CompanyId IS NULL
    THROW 50001, 'Annapurna main company is not configured.', 1;

DECLARE @OfficeId INT =
(
    SELECT OfficeId
    FROM dbo.tbl_company
    WHERE CompanyId = @CompanyId
);

DECLARE @ShyamaId INT =
(
    SELECT TOP (1) SubCompanyId
    FROM dbo.tbl_subcompany
    WHERE CompanyId = @CompanyId AND SubCompanyCode IN ('SHYAMA', 'UNIT-1')
    ORDER BY CASE WHEN SubCompanyCode = 'SHYAMA' THEN 0 ELSE 1 END
);

IF @ShyamaId IS NULL
BEGIN
    INSERT dbo.tbl_subcompany (CompanyId, SubCompanyCode, SubCompanyName)
    VALUES (@CompanyId, 'SHYAMA', 'Shyama Enterprises');
    SET @ShyamaId = CONVERT(INT, SCOPE_IDENTITY());
END
ELSE
    UPDATE dbo.tbl_subcompany
    SET SubCompanyCode = 'SHYAMA', SubCompanyName = 'Shyama Enterprises', IsActive = 1, ModifiedDate = SYSDATETIME()
    WHERE SubCompanyId = @ShyamaId;

DECLARE @PoojaId INT =
(
    SELECT TOP (1) SubCompanyId
    FROM dbo.tbl_subcompany
    WHERE CompanyId = @CompanyId AND SubCompanyCode IN ('POOJA', 'UNIT-2')
    ORDER BY CASE WHEN SubCompanyCode = 'POOJA' THEN 0 ELSE 1 END
);

IF @PoojaId IS NULL
BEGIN
    INSERT dbo.tbl_subcompany (CompanyId, SubCompanyCode, SubCompanyName)
    VALUES (@CompanyId, 'POOJA', 'Pooja Enterprises');
    SET @PoojaId = CONVERT(INT, SCOPE_IDENTITY());
END
ELSE
    UPDATE dbo.tbl_subcompany
    SET SubCompanyCode = 'POOJA', SubCompanyName = 'Pooja Enterprises', IsActive = 1, ModifiedDate = SYSDATETIME()
    WHERE SubCompanyId = @PoojaId;

DECLARE @HaudiLocationId INT =
(
    SELECT TOP (1) LocationId
    FROM dbo.m_LocationMaster
    WHERE UPPER(REPLACE(LocationName, ' ', '')) IN ('HAUDI', 'HAUDY')
    ORDER BY LocationId
);

IF @HaudiLocationId IS NULL
BEGIN
    DECLARE @LocationSuffix INT =
    (
        SELECT ISNULL(MAX(TRY_CONVERT(INT, SUBSTRING(LocationCode, 5, 20))), 0) + 1
        FROM dbo.m_LocationMaster
    );

    INSERT dbo.m_LocationMaster
        (LocationCode, LocationName, LocationType, Capacity, CapacityUnit, IsActive, CreatedBy, CreatedDate, SubCompanyId)
    VALUES
        (CONCAT('LOC-', @LocationSuffix), 'HAUDI', 'Haudi', 0, 'MT', 1, 1, GETDATE(), @ShyamaId);

    SET @HaudiLocationId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT dbo.t_OfficeLocationMapping
        (OfficeId, LocationId, UnloadingAllowed, EffectiveFrom, IsActive, CreatedBy, CreatedDate)
    VALUES
        (@OfficeId, @HaudiLocationId, 1, CONVERT(DATE, GETDATE()), 1, 1, GETDATE());
END;

UPDATE dbo.m_LocationMaster
SET SubCompanyId = CASE WHEN LocationId = @HaudiLocationId THEN @ShyamaId ELSE @PoojaId END,
    ModifiedDate = GETDATE()
WHERE IsActive = 1;

COMMIT;

SELECT subcompany.SubCompanyName, location.LocationId, location.LocationName
FROM dbo.tbl_subcompany subcompany
LEFT JOIN dbo.m_LocationMaster location ON location.SubCompanyId = subcompany.SubCompanyId AND location.IsActive = 1
WHERE subcompany.CompanyId = @CompanyId
ORDER BY subcompany.SubCompanyName, location.LocationId;
