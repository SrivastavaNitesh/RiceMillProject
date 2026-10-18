SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.m_InwardTypeMaster)
    BEGIN
        SET IDENTITY_INSERT dbo.m_InwardTypeMaster ON;
        INSERT dbo.m_InwardTypeMaster
            (InwardTypeId, InwardTypeName, RSTRequired, IsActive)
        VALUES
            (1, N'Paddy Inward', 1, 1),
            (2, N'Spare Parts Inward', 0, 1),
            (3, N'Packing Material Inward', 0, 1),
            (4, N'Stationery Inward', 0, 1),
            (5, N'Other Inward', 0, 1);
        SET IDENTITY_INSERT dbo.m_InwardTypeMaster OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.m_VehicleTypeMaster)
    BEGIN
        SET IDENTITY_INSERT dbo.m_VehicleTypeMaster ON;
        INSERT dbo.m_VehicleTypeMaster
            (VehicleTypeId, VehicleTypeName, IsActive)
        VALUES
            (1, N'Truck', 1),
            (2, N'Tractor', 1),
            (3, N'Trolley', 1),
            (4, N'Mini Truck', 1),
            (5, N'Other', 1);
        SET IDENTITY_INSERT dbo.m_VehicleTypeMaster OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.m_StatusMaster)
    BEGIN
        INSERT dbo.m_StatusMaster (StatusId, StatusCode, StatusName, IsActive)
        VALUES
            (1, N'GADI_IN', N'Gadi In', 1),
            (2, N'WEIGHT_DONE', N'Weight Done', 1),
            (3, N'UNLOAD_ASSIGNED', N'Unload Assigned', 1),
            (4, N'UNLOADED', N'Unloaded', 1),
            (5, N'UNLOAD_VERIFIED', N'Unload Verified', 1),
            (6, N'TESTED', N'Quality Tested', 1),
            (7, N'EXITED', N'Exited', 1),
            (8, N'SETTLED', N'Settled', 1),
            (9, N'OUTWARD_PENDING', N'Outward Pending Approval', 1),
            (10, N'OUTWARD_APPROVED', N'Outward Approved', 1),
            (11, N'OUTWARD_REVERTED', N'Outward Reverted', 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.t_SerialCounters)
    BEGIN
        INSERT dbo.t_SerialCounters (CounterType, LastNo)
        VALUES
            (N'GLOBAL', 0),
            (N'INWARD', 0),
            (N'OUTWARD', 0),
            (N'RST', 0);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.m_RSTStageMaster)
    BEGIN
        SET IDENTITY_INSERT dbo.m_RSTStageMaster ON;
        INSERT dbo.m_RSTStageMaster
            (StageId, StageCode, StageName, SequenceNo, IsActive)
        VALUES
            (3, N'SUPERVISOR_ASSIGNMENT', N'Supervisor Assignment', 3, 1),
            (6, N'LAB_REPORT', N'Lab Report', 6, 1),
            (7, N'TARE_WEIGHT', N'Tare Weight', 7, 1),
            (9, N'COMPLETED', N'Completed', 9, 1),
            (10, N'RST_GENERATED', N'RST Generated / Weightman', 1, 1),
            (12, N'SUPERVISOR_FINAL', N'Supervisor Final Details', 4, 1),
            (13, N'GATE_OUTWARD', N'Gate Outward', 7, 1),
            (14, N'LAB_REPORT_PARTIAL', N'Lab Report Partially Completed', 8, 1),
            (15, N'LAB_REPORT_COMPLETED', N'Lab Report Fully Completed', 9, 1),
            (16, N'BILL_GENERATED', N'Bill Generated', 10, 1);
        SET IDENTITY_INSERT dbo.m_RSTStageMaster OFF;
    END;

    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;
GO

SELECT N'm_InwardTypeMaster' TableName, COUNT(*) [RowCount] FROM dbo.m_InwardTypeMaster
UNION ALL SELECT N'm_VehicleTypeMaster', COUNT(*) FROM dbo.m_VehicleTypeMaster
UNION ALL SELECT N'm_StatusMaster', COUNT(*) FROM dbo.m_StatusMaster
UNION ALL SELECT N't_SerialCounters', COUNT(*) FROM dbo.t_SerialCounters
UNION ALL SELECT N'm_RSTStageMaster', COUNT(*) FROM dbo.m_RSTStageMaster;
