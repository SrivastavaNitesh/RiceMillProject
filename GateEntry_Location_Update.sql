USE RiceMillDB;
GO

IF COL_LENGTH('dbo.t_GateEntry', 'TargetOfficeId') IS NULL
BEGIN
    ALTER TABLE dbo.t_GateEntry ADD TargetOfficeId INT NULL;
    ALTER TABLE dbo.t_GateEntry ADD TargetLocationId INT NULL;
END
GO

CREATE OR ALTER PROCEDURE sp_CreateGateEntry
    @VehicleId INT,
    @PartyId INT,
    @DriverId INT,
    @GrossWeight DECIMAL(18,2),
    @TargetOfficeId INT,
    @TargetLocationId INT
AS
BEGIN
    DECLARE @RSTNumber NVARCHAR(50) = 'RST-' + REPLACE(CONVERT(NVARCHAR, GETDATE(), 112), '-', '') + '-' + CAST((ABS(CHECKSUM(NEWID())) % 10000) AS NVARCHAR);
    
    INSERT INTO t_GateEntry (RSTNumber, VehicleId, PartyId, DriverId, GrossWeight, TargetOfficeId, TargetLocationId)
    VALUES (@RSTNumber, @VehicleId, @PartyId, @DriverId, @GrossWeight, @TargetOfficeId, @TargetLocationId);
    
    SELECT @RSTNumber AS RSTNumber;
END
GO
