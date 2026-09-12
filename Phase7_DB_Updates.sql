USE RiceMillDB;
GO

-- 1. Create Vehicle Mapping Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[m_VehicleMapping]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[m_VehicleMapping] (
        [MappingId] INT IDENTITY(1,1) PRIMARY KEY,
        [VehicleId] INT NOT NULL,
        [PartyId] INT NOT NULL,  -- Logical link to p02_Person
        [DriverId] INT NOT NULL, -- Logical link to p02_Person
        [IsActive] BIT DEFAULT 1
    );
END
GO

-- 2. Stored Procedure to Insert Vehicle Mapping
CREATE OR ALTER PROCEDURE sp_InsertVehicleMapping
    @VehicleId INT,
    @PartyId INT,
    @DriverId INT
AS
BEGIN
    -- Deactivate old mappings for this vehicle to keep 1 active at a time, or allow multiple active
    -- The user said "multiple linking ho sakte hai", so we just insert a new active one.
    INSERT INTO m_VehicleMapping (VehicleId, PartyId, DriverId)
    VALUES (@VehicleId, @PartyId, @DriverId)
END
GO

-- 3. Stored Procedure to Create User Login
CREATE OR ALTER PROCEDURE sp_CreateUserLogin
    @PersonId INT,
    @MobileNumber NVARCHAR(20),
    @PersonName NVARCHAR(200)
AS
BEGIN
    -- Only create if not exists
    IF NOT EXISTS (SELECT 1 FROM sa05_user WHERE PersonId = @PersonId)
    BEGIN
        DECLARE @Username NVARCHAR(100);
        
        IF (@MobileNumber IS NOT NULL AND @MobileNumber <> '')
            SET @Username = @MobileNumber;
        ELSE
            SET @Username = REPLACE(@PersonName, ' ', '') + CAST(@PersonId AS NVARCHAR(10));
            
        -- Ensure username is unique
        IF EXISTS (SELECT 1 FROM sa05_user WHERE Username = @Username)
        BEGIN
             SET @Username = @Username + CAST(@PersonId AS NVARCHAR(10));
        END
        
        -- Password is set to '1111' by default
        INSERT INTO sa05_user (Username, PasswordHash, PersonId)
        VALUES (@Username, '1111', @PersonId)
    END
END
GO

-- 4. Dashboard Stats Stored Procedure
CREATE OR ALTER PROCEDURE sp_GetDashboardStats
AS
BEGIN
    DECLARE @TotalEntered INT = 0;
    DECLARE @PendingUnload INT = 0;
    DECLARE @PendingLab INT = 0;
    DECLARE @PendingSettlement INT = 0;
    
    -- Trucks inside (Status = 'Entered')
    SELECT @TotalEntered = COUNT(*) FROM t_GateEntry WHERE Status = 'Entered';
    
    -- Trucks pending unload (Status = 'Entered' meaning they haven't unloaded yet, or 'Unloaded' if we want a different metric)
    -- Actually, if they are Entered, they need Unloading.
    SELECT @PendingUnload = COUNT(*) FROM t_GateEntry WHERE Status = 'Entered';
    
    -- Trucks pending Lab (Status = 'Unloaded' or 'Exited' without Lab)
    SELECT @PendingLab = COUNT(*) FROM t_GateEntry WHERE Status IN ('Unloaded', 'Exited') AND RSTNumber NOT IN (SELECT RSTNumber FROM t_LabQualityCheck);
    
    -- Trucks pending settlement (Status = 'Exited' and has Lab done, or just Exited and not billed)
    SELECT @PendingSettlement = COUNT(*) FROM t_GateEntry WHERE Status = 'Exited';
    
    SELECT 
        @TotalEntered AS TotalEntered,
        @PendingUnload AS PendingUnload,
        @PendingLab AS PendingLab,
        @PendingSettlement AS PendingSettlement;
END
GO

-- 5. Stored Procedure to Get Party and Driver by Vehicle
CREATE OR ALTER PROCEDURE sp_GetPartiesAndDriversByVehicle
    @VehicleId INT
AS
BEGIN
    SELECT 
        vm.MappingId,
        p.PersonId AS PartyId,
        p.PersonName AS PartyName,
        d.PersonId AS DriverId,
        d.PersonName AS DriverName,
        d.MobileNumber AS DriverMobile
    FROM m_VehicleMapping vm
    INNER JOIN p02_Person p ON vm.PartyId = p.PersonId
    INNER JOIN p02_Person d ON vm.DriverId = d.PersonId
    WHERE vm.VehicleId = @VehicleId AND vm.IsActive = 1
END
GO
