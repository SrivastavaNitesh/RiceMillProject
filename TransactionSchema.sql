USE RiceMillDB;
GO

-- 1. Create Transaction Tables
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_GateEntry]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_GateEntry] (
        [RSTNumber] NVARCHAR(50) PRIMARY KEY, -- Primary Identifier generated at Gate
        [VehicleId] INT NOT NULL,             -- Links to m_Vehicle
        [PartyId] INT NOT NULL,               -- Links to p02_Person (Kisan/Gov)
        [DriverId] INT NOT NULL,              -- Links to p02_Person (Driver)
        [GrossWeight] DECIMAL(18,2) NOT NULL, -- Weight of loaded truck
        [GateEntryTime] DATETIME DEFAULT GETDATE(),
        [TareWeight] DECIMAL(18,2),           -- Weight of empty truck
        [NetWeight] DECIMAL(18,2),            -- Gross - Tare
        [GateExitTime] DATETIME,
        [Status] NVARCHAR(50) DEFAULT 'Entered' -- Entered, Unloaded, Tested, Exited, Settled
    );
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_UnloadTransaction]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_UnloadTransaction] (
        [UnloadId] INT IDENTITY(1,1) PRIMARY KEY,
        [RSTNumber] NVARCHAR(50) NOT NULL,    -- Links to t_GateEntry
        [LocationId] INT NOT NULL,            -- Links to o05_office
        [SupervisorId] INT NOT NULL,          -- Links to p02_Person
        [MethId] INT NOT NULL,                -- Links to p02_Person
        [BagTypeId] INT NOT NULL,             -- Links to m_BagType
        [NumberOfBags] INT NOT NULL,
        [TotalBagDeductionGrams] DECIMAL(18,2) NOT NULL, -- NumberOfBags * BagType deduction
        [UnloadTime] DATETIME DEFAULT GETDATE()
    );
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_WorkerAllocation]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_WorkerAllocation] (
        [AllocationId] INT IDENTITY(1,1) PRIMARY KEY,
        [UnloadId] INT NOT NULL,              -- Links to t_UnloadTransaction
        [WorkerId] INT NOT NULL,              -- Links to p02_Person
        [PalledariAmount] DECIMAL(18,2) NOT NULL, -- 10 Rs day / 15 Rs night
        [AllocationTime] DATETIME DEFAULT GETDATE()
    );
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_LabQualityCheck]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_LabQualityCheck] (
        [LabCheckId] INT IDENTITY(1,1) PRIMARY KEY,
        [RSTNumber] NVARCHAR(50) NOT NULL,    -- Links to t_GateEntry
        [MoisturePct] DECIMAL(5,2) NOT NULL,
        [DustPct] DECIMAL(5,2) NOT NULL,
        [PayiaPct] DECIMAL(5,2) NOT NULL,
        [GrainQualityPct] DECIMAL(5,2) NOT NULL,
        [TotalDeductionPct] DECIMAL(5,2) NOT NULL,
        [TestedBy] INT NOT NULL,              -- Links to p02_Person (Lab Tech)
        [TestTime] DATETIME DEFAULT GETDATE()
    );
END

GO

-- 2. Stored Procedures for Gate Entry
CREATE OR ALTER PROCEDURE sp_CreateGateEntry
    @RSTNumber NVARCHAR(50),
    @VehicleId INT,
    @PartyId INT,
    @DriverId INT,
    @GrossWeight DECIMAL(18,2)
AS
BEGIN
    INSERT INTO t_GateEntry (RSTNumber, VehicleId, PartyId, DriverId, GrossWeight, Status)
    VALUES (@RSTNumber, @VehicleId, @PartyId, @DriverId, @GrossWeight, 'Entered');
END
GO

CREATE OR ALTER PROCEDURE sp_CompleteGateExit
    @RSTNumber NVARCHAR(50),
    @TareWeight DECIMAL(18,2)
AS
BEGIN
    UPDATE t_GateEntry
    SET TareWeight = @TareWeight,
        NetWeight = GrossWeight - @TareWeight,
        GateExitTime = GETDATE(),
        Status = 'Exited'
    WHERE RSTNumber = @RSTNumber;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllGateEntries
AS
BEGIN
    SELECT g.RSTNumber, g.GrossWeight, g.TareWeight, g.NetWeight, g.GateEntryTime, g.GateExitTime, g.Status,
           v.VehicleNumber, p.PersonName AS PartyName, d.PersonName AS DriverName
    FROM t_GateEntry g
    LEFT JOIN m_Vehicle v ON g.VehicleId = v.VehicleId
    LEFT JOIN p02_Person p ON g.PartyId = p.PersonId
    LEFT JOIN p02_Person d ON g.DriverId = d.PersonId
    ORDER BY g.GateEntryTime DESC;
END
GO

-- 3. Stored Procedures for Unloading
CREATE OR ALTER PROCEDURE sp_SaveUnloading
    @RSTNumber NVARCHAR(50),
    @LocationId INT,
    @SupervisorId INT,
    @MethId INT,
    @BagTypeId INT,
    @NumberOfBags INT
AS
BEGIN
    DECLARE @DeductionGrams INT;
    SELECT @DeductionGrams = DeductionWeightGrams FROM m_BagType WHERE BagTypeId = @BagTypeId;
    
    DECLARE @TotalDeduction DECIMAL(18,2) = (@NumberOfBags * @DeductionGrams) / 1000.0; -- Convert grams to KG
    
    INSERT INTO t_UnloadTransaction (RSTNumber, LocationId, SupervisorId, MethId, BagTypeId, NumberOfBags, TotalBagDeductionGrams)
    VALUES (@RSTNumber, @LocationId, @SupervisorId, @MethId, @BagTypeId, @NumberOfBags, @TotalDeduction);
    
    SELECT SCOPE_IDENTITY() AS UnloadId;
END
GO

CREATE OR ALTER PROCEDURE sp_SaveWorkerAllocation
    @UnloadId INT,
    @WorkerId INT,
    @PalledariAmount DECIMAL(18,2)
AS
BEGIN
    INSERT INTO t_WorkerAllocation (UnloadId, WorkerId, PalledariAmount)
    VALUES (@UnloadId, @WorkerId, @PalledariAmount);
END
GO

-- 4. Stored Procedure for Lab Check
CREATE OR ALTER PROCEDURE sp_SaveLabQualityCheck
    @RSTNumber NVARCHAR(50),
    @MoisturePct DECIMAL(5,2),
    @DustPct DECIMAL(5,2),
    @PayiaPct DECIMAL(5,2),
    @GrainQualityPct DECIMAL(5,2),
    @TotalDeductionPct DECIMAL(5,2),
    @TestedBy INT
AS
BEGIN
    INSERT INTO t_LabQualityCheck (RSTNumber, MoisturePct, DustPct, PayiaPct, GrainQualityPct, TotalDeductionPct, TestedBy)
    VALUES (@RSTNumber, @MoisturePct, @DustPct, @PayiaPct, @GrainQualityPct, @TotalDeductionPct, @TestedBy);
    
    UPDATE t_GateEntry SET Status = 'Tested' WHERE RSTNumber = @RSTNumber;
END
GO
