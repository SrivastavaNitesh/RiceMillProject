USE RiceMillDB;
GO

-- 1. Create Office-Location Mapping Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[o_OfficeLocation]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[o_OfficeLocation] (
        [LocationId] INT IDENTITY(1,1) PRIMARY KEY,
        [OfficeId] INT NOT NULL,
        [LocationName] NVARCHAR(200) NOT NULL,
        [IsActive] BIT DEFAULT 1
    );
END
GO

-- 2. Create Person-Location Mapping Table (For Supervisors/Gatemen)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[p_PersonLocation]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[p_PersonLocation] (
        [MappingId] INT IDENTITY(1,1) PRIMARY KEY,
        [PersonId] INT NOT NULL,
        [LocationId] INT NOT NULL,
        [IsActive] BIT DEFAULT 1
    );
END
GO

-- 3. Modify Unload Transaction Table to add GateManId
IF COL_LENGTH('dbo.t_UnloadTransaction', 'GateManId') IS NULL
BEGIN
    ALTER TABLE dbo.t_UnloadTransaction ADD GateManId INT NULL;
END
GO

-- 4. Create Unload-Location Mapping Table (A truck can unload at multiple locations)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_UnloadLocation]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_UnloadLocation] (
        [UnloadLocationId] INT IDENTITY(1,1) PRIMARY KEY,
        [UnloadId] INT NOT NULL,
        [LocationId] INT NOT NULL,
        [IsActive] BIT DEFAULT 1
    );
END
GO

-- 5. Create Stored Procedures for Office Locations
CREATE OR ALTER PROCEDURE sp_InsertOfficeLocation
    @OfficeId INT,
    @LocationName NVARCHAR(200)
AS
BEGIN
    INSERT INTO o_OfficeLocation (OfficeId, LocationName)
    VALUES (@OfficeId, @LocationName)
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_GetOfficeLocations
    @OfficeId INT
AS
BEGIN
    SELECT LocationId, OfficeId, LocationName, IsActive 
    FROM o_OfficeLocation 
    WHERE OfficeId = @OfficeId AND IsActive = 1
END
GO

-- 6. Create Stored Procedures for Person Locations
CREATE OR ALTER PROCEDURE sp_InsertPersonLocation
    @PersonId INT,
    @LocationId INT
AS
BEGIN
    INSERT INTO p_PersonLocation (PersonId, LocationId)
    VALUES (@PersonId, @LocationId)
END
GO

CREATE OR ALTER PROCEDURE sp_GetPersonLocations
    @PersonId INT
AS
BEGIN
    SELECT pl.MappingId, pl.PersonId, pl.LocationId, ol.LocationName
    FROM p_PersonLocation pl
    INNER JOIN o_OfficeLocation ol ON pl.LocationId = ol.LocationId
    WHERE pl.PersonId = @PersonId AND pl.IsActive = 1
END
GO

-- 7. Modify SaveUnloading SP to accept GateManId
-- Assuming sp_SaveUnloading exists from earlier phases, we'll recreate or alter it
CREATE OR ALTER PROCEDURE sp_SaveUnloading
    @RSTNumber NVARCHAR(50),
    @SupervisorId INT,
    @GateManId INT, -- Added parameter
    @MethId INT,
    @BagTypeId INT,
    @NumberOfBags INT,
    @TotalBagDeductionGrams DECIMAL(18,2)
AS
BEGIN
    INSERT INTO t_UnloadTransaction (
        RSTNumber, SupervisorId, GateManId, MethId, BagTypeId, 
        NumberOfBags, TotalBagDeductionGrams, UnloadTime
    )
    VALUES (
        @RSTNumber, @SupervisorId, @GateManId, @MethId, @BagTypeId, 
        @NumberOfBags, @TotalBagDeductionGrams, GETDATE()
    )
    
    DECLARE @UnloadId INT = SCOPE_IDENTITY();
    
    -- Update Gate Entry Status
    UPDATE t_GateEntry SET Status = 'Unloaded' WHERE RSTNumber = @RSTNumber;
    
    SELECT @UnloadId AS UnloadId;
END
GO

-- 8. Create SP for Unload Locations
CREATE OR ALTER PROCEDURE sp_InsertUnloadLocation
    @UnloadId INT,
    @LocationId INT
AS
BEGIN
    INSERT INTO t_UnloadLocation (UnloadId, LocationId)
    VALUES (@UnloadId, @LocationId)
END
GO
