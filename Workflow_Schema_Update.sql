USE RiceMillDB;
GO

-- 1. Create table for multiple target locations per Gate Entry
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_GateEntryLocations]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.t_GateEntryLocations (
        RSTNumber NVARCHAR(50) NOT NULL,
        LocationId INT NOT NULL,
        FOREIGN KEY (RSTNumber) REFERENCES t_GateEntry(RSTNumber)
    );
END
GO

-- 2. Alter UnloadTransaction to support new workflow states and nullable fields
IF COL_LENGTH('dbo.t_UnloadTransaction', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.t_UnloadTransaction ADD Status NVARCHAR(20) DEFAULT 'Assigned';
END
GO

-- Make Unload details nullable since they are filled later by Meth
ALTER TABLE dbo.t_UnloadTransaction ALTER COLUMN BagTypeId INT NULL;
ALTER TABLE dbo.t_UnloadTransaction ALTER COLUMN NumberOfBags INT NULL;
ALTER TABLE dbo.t_UnloadTransaction ALTER COLUMN TotalBagDeductionGrams DECIMAL(18,2) NULL;
ALTER TABLE dbo.t_UnloadTransaction ALTER COLUMN UnloadTime DATETIME NULL;
GO

-- 3. Update Stored Procedure for Gate Entry
CREATE OR ALTER PROCEDURE sp_CreateGateEntry
    @RSTNumber NVARCHAR(50),
    @VehicleId INT,
    @PartyId INT,
    @DriverId INT,
    @GrossWeight DECIMAL(18,2),
    @TargetOfficeId INT
AS
BEGIN
    INSERT INTO t_GateEntry (RSTNumber, VehicleId, PartyId, DriverId, GrossWeight, TargetOfficeId)
    VALUES (@RSTNumber, @VehicleId, @PartyId, @DriverId, @GrossWeight, @TargetOfficeId);
    
    SELECT @RSTNumber AS RSTNumber;
END
GO

-- 4. Create SP for GateEntryLocations
CREATE OR ALTER PROCEDURE sp_InsertGateEntryLocation
    @RSTNumber NVARCHAR(50),
    @LocationId INT
AS
BEGIN
    INSERT INTO t_GateEntryLocations (RSTNumber, LocationId)
    VALUES (@RSTNumber, @LocationId);
END
GO

-- 5. Create SP for Supervisor Assignment
CREATE OR ALTER PROCEDURE sp_AssignUnloading
    @RSTNumber NVARCHAR(50),
    @SupervisorId INT,
    @MethId INT
AS
BEGIN
    DECLARE @UnloadId INT;
    
    INSERT INTO t_UnloadTransaction (RSTNumber, SupervisorId, MethId, Status)
    VALUES (@RSTNumber, @SupervisorId, @MethId, 'Assigned');
    
    SET @UnloadId = SCOPE_IDENTITY();
    SELECT @UnloadId AS UnloadId;
END
GO

-- 6. Create SP for Meth Unloading Submission
CREATE OR ALTER PROCEDURE sp_SubmitUnloading
    @UnloadId INT,
    @GateManId INT,
    @BagTypeId INT,
    @NumberOfBags INT
AS
BEGIN
    UPDATE t_UnloadTransaction 
    SET GateManId = @GateManId,
        BagTypeId = @BagTypeId,
        NumberOfBags = @NumberOfBags,
        UnloadTime = GETDATE(),
        Status = 'Unloaded'
    WHERE UnloadId = @UnloadId;
END
GO

-- 7. Create SP for Supervisor Verification
CREATE OR ALTER PROCEDURE sp_VerifyUnloading
    @UnloadId INT,
    @TotalBagDeductionGrams DECIMAL(18,2)
AS
BEGIN
    UPDATE t_UnloadTransaction 
    SET TotalBagDeductionGrams = @TotalBagDeductionGrams,
        Status = 'Verified'
    WHERE UnloadId = @UnloadId;
END
GO

-- 8. Fix sp_GetAllGateEntries to include TargetOfficeId
CREATE OR ALTER PROCEDURE sp_GetAllGateEntries
AS
BEGIN
    SELECT g.RSTNumber, g.GrossWeight, g.TareWeight, g.NetWeight, g.GateEntryTime, g.GateExitTime, g.Status, g.TargetOfficeId,
           v.VehicleNumber, p.PersonName AS PartyName, d.PersonName AS DriverName
    FROM t_GateEntry g
    LEFT JOIN m_Vehicle v ON g.VehicleId = v.VehicleId
    LEFT JOIN p02_Person p ON g.PartyId = p.PersonId
    LEFT JOIN p02_Person d ON g.DriverId = d.PersonId
    ORDER BY g.GateEntryTime DESC;
END
GO
