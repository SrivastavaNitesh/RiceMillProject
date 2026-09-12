USE [RiceMillDB];
GO

-- ============================================================================
-- 1. LOCATION MASTER TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'm_LocationMaster')
BEGIN
    CREATE TABLE m_LocationMaster (
        LocationId INT IDENTITY(1,1) PRIMARY KEY,
        LocationCode NVARCHAR(20) NOT NULL UNIQUE,
        LocationName NVARCHAR(100) NOT NULL,
        LocationType NVARCHAR(50) NOT NULL,
        Capacity DECIMAL(18,2) NULL DEFAULT 0,
        CapacityUnit NVARCHAR(10) NULL,
        Description NVARCHAR(250) NULL,
        Address NVARCHAR(250) NULL,
        Remarks NVARCHAR(250) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL
    );
END
GO

-- ============================================================================
-- 2. OFFICE - LOCATION MAPPING TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 't_OfficeLocationMapping')
BEGIN
    CREATE TABLE t_OfficeLocationMapping (
        MappingId INT IDENTITY(1,1) PRIMARY KEY,
        OfficeId INT NOT NULL,
        LocationId INT NOT NULL,
        UnloadingAllowed BIT NOT NULL DEFAULT 1,
        EffectiveFrom DATE NULL DEFAULT GETDATE(),
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL,
        CONSTRAINT UQ_OfficeLocation UNIQUE (OfficeId, LocationId)
    );
END
GO

-- ============================================================================
-- 3. STORED PROCEDURE: MANAGE LOCATION MASTER
-- ============================================================================
CREATE OR ALTER PROCEDURE sp_ManageLocationMaster
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
    @CreatedBy INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'INSERT'
    BEGIN
        -- Generate Auto Location Code (LOC-001, LOC-002...)
        DECLARE @NextId INT;
        SELECT @NextId = ISNULL(MAX(LocationId), 0) + 1 FROM m_LocationMaster;
        SET @LocationCode = 'LOC-' + RIGHT('000' + CAST(@NextId AS VARCHAR(10)), 3);

        INSERT INTO m_LocationMaster (
            LocationCode, LocationName, LocationType, Capacity, CapacityUnit,
            Description, Address, Remarks, IsActive, CreatedBy, CreatedDate
        )
        VALUES (
            @LocationCode, @LocationName, @LocationType, ISNULL(@Capacity, 0), @CapacityUnit,
            @Description, @Address, @Remarks, ISNULL(@IsActive, 1), @CreatedBy, GETDATE()
        );

        SELECT SCOPE_IDENTITY() AS LocationId, @LocationCode AS LocationCode;
    END
    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE m_LocationMaster
        SET LocationName = @LocationName,
            LocationType = @LocationType,
            Capacity = ISNULL(@Capacity, 0),
            CapacityUnit = @CapacityUnit,
            Description = @Description,
            Address = @Address,
            Remarks = @Remarks,
            IsActive = @IsActive,
            ModifiedBy = @CreatedBy,
            ModifiedDate = GETDATE()
        WHERE LocationId = @LocationId;
    END
    ELSE IF @Action = 'SELECT_ALL'
    BEGIN
        SELECT * FROM m_LocationMaster ORDER BY LocationId DESC;
    END
    ELSE IF @Action = 'SELECT_BY_ID'
    BEGIN
        SELECT * FROM m_LocationMaster WHERE LocationId = @LocationId;
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        UPDATE m_LocationMaster SET IsActive = 0 WHERE LocationId = @LocationId;
    END
END
GO

-- ============================================================================
-- 4. STORED PROCEDURE: MANAGE OFFICE LOCATION MAPPING
-- ============================================================================
CREATE OR ALTER PROCEDURE sp_ManageOfficeLocationMapping
    @Action VARCHAR(20),
    @MappingId INT = NULL,
    @OfficeId INT = NULL,
    @LocationId INT = NULL,
    @UnloadingAllowed BIT = 1,
    @EffectiveFrom DATE = NULL,
    @IsActive BIT = 1,
    @CreatedBy INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'INSERT'
    BEGIN
        IF EXISTS (SELECT 1 FROM t_OfficeLocationMapping WHERE OfficeId = @OfficeId AND LocationId = @LocationId)
        BEGIN
            UPDATE t_OfficeLocationMapping
            SET UnloadingAllowed = @UnloadingAllowed,
                EffectiveFrom = ISNULL(@EffectiveFrom, GETDATE()),
                IsActive = ISNULL(@IsActive, 1),
                ModifiedBy = @CreatedBy,
                ModifiedDate = GETDATE()
            WHERE OfficeId = @OfficeId AND LocationId = @LocationId;
        END
        ELSE
        BEGIN
            INSERT INTO t_OfficeLocationMapping (
                OfficeId, LocationId, UnloadingAllowed, EffectiveFrom, IsActive, CreatedBy, CreatedDate
            )
            VALUES (
                @OfficeId, @LocationId, @UnloadingAllowed, ISNULL(@EffectiveFrom, GETDATE()), ISNULL(@IsActive, 1), @CreatedBy, GETDATE()
            );
        END
    END
    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE t_OfficeLocationMapping
        SET OfficeId = @OfficeId,
            LocationId = @LocationId,
            UnloadingAllowed = @UnloadingAllowed,
            EffectiveFrom = @EffectiveFrom,
            IsActive = @IsActive,
            ModifiedBy = @CreatedBy,
            ModifiedDate = GETDATE()
        WHERE MappingId = @MappingId;
    END
    ELSE IF @Action = 'SELECT_ALL'
    BEGIN
        SELECT 
            m.MappingId,
            m.OfficeId,
            o.o10_postname AS OfficeName,
            m.LocationId,
            l.LocationName,
            l.LocationCode,
            m.UnloadingAllowed,
            m.EffectiveFrom,
            m.IsActive
        FROM t_OfficeLocationMapping m
        INNER JOIN o10_post o ON m.OfficeId = o.o10_postid
        INNER JOIN m_LocationMaster l ON m.LocationId = l.LocationId
        ORDER BY m.MappingId DESC;
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        DELETE FROM t_OfficeLocationMapping WHERE MappingId = @MappingId;
    END
END
GO

-- Seed sample data if empty
IF NOT EXISTS (SELECT 1 FROM m_LocationMaster)
BEGIN
    EXEC sp_ManageLocationMaster @Action='INSERT', @LocationName='Godown-01', @LocationType='Godown', @Capacity=5000, @CapacityUnit='MT', @Description='Main Grain Godown';
    EXEC sp_ManageLocationMaster @Action='INSERT', @LocationName='Paddy Yard', @LocationType='Yard', @Capacity=10000, @CapacityUnit='MT', @Description='Raw Paddy Holding Area';
    EXEC sp_ManageLocationMaster @Action='INSERT', @LocationName='Shed-01', @LocationType='Shed', @Capacity=3000, @CapacityUnit='MT', @Description='Storage Shed';
    EXEC sp_ManageLocationMaster @Action='INSERT', @LocationName='Godown-02', @LocationType='Godown', @Capacity=7000, @CapacityUnit='MT', @Description='Finished Rice Warehouse';
    EXEC sp_ManageLocationMaster @Action='INSERT', @LocationName='Temporary Yard', @LocationType='Yard', @Capacity=4000, @CapacityUnit='MT', @Description='Overflow Yard';
END
GO
