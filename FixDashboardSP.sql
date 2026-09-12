USE RiceMillDB;
GO

-- SP: Dashboard Stats (Using StatusId from m_StatusMaster)
CREATE OR ALTER PROCEDURE sp_GetDashboardStats
AS
BEGIN
    DECLARE @TotalEntered INT = 0;
    DECLARE @PendingUnload INT = 0;
    DECLARE @PendingLab INT = 0;
    DECLARE @PendingSettlement INT = 0;
    
    -- Trucks inside (StatusId = 1 : Gadi In)
    SELECT @TotalEntered = COUNT(*) FROM t_GateEntry WHERE StatusId = 1;
    
    -- Trucks pending unload (StatusId = 1)
    SELECT @PendingUnload = COUNT(*) FROM t_GateEntry WHERE StatusId = 1;
    
    -- Trucks pending Lab (StatusId IN (4, 7) meaning Unloaded or Exited)
    SELECT @PendingLab = COUNT(*) FROM t_GateEntry WHERE StatusId IN (4, 7) AND RSTNumber NOT IN (SELECT RSTNumber FROM t_LabQualityCheck);
    
    -- Trucks pending settlement (StatusId = 7)
    SELECT @PendingSettlement = COUNT(*) FROM t_GateEntry WHERE StatusId = 7;
    
    SELECT 
        @TotalEntered AS TotalEntered,
        @PendingUnload AS PendingUnload,
        @PendingLab AS PendingLab,
        @PendingSettlement AS PendingSettlement;
END
GO

-- SP: Fix sp_GetAllGateEntries using StatusId
CREATE OR ALTER PROCEDURE sp_GetAllGateEntries
AS
BEGIN
    SELECT g.RSTNumber, g.GrossWeight, g.TareWeight, g.NetWeight, g.GateEntryTime, g.GateExitTime, 
           sm.StatusName AS Status, g.StatusId, g.TargetOfficeId,
           v.VehicleNumber, p.PersonName AS PartyName, d.PersonName AS DriverName
    FROM t_GateEntry g
    LEFT JOIN m_Vehicle v ON g.VehicleId = v.VehicleId
    LEFT JOIN p02_Person p ON g.PartyId = p.PersonId
    LEFT JOIN p02_Person d ON g.DriverId = d.PersonId
    LEFT JOIN m_StatusMaster sm ON g.StatusId = sm.StatusId
    ORDER BY g.GateEntryTime DESC;
END
GO
