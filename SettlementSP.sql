USE RiceMillDB;
GO

-- Stored Procedure to generate Bill / Settlement
CREATE OR ALTER PROCEDURE sp_GetSettlementDetails
    @RSTNumber NVARCHAR(50)
AS
BEGIN
    -- This SP calculates the final bill details for a given RST
    SELECT 
        g.RSTNumber,
        p.PersonName AS PartyName,
        v.VehicleNumber,
        g.GrossWeight,
        g.TareWeight,
        g.NetWeight,
        ISNULL((SELECT SUM(TotalBagDeductionGrams) / 1000.0 FROM t_UnloadTransaction WHERE RSTNumber = g.RSTNumber), 0) AS TotalBagDeductionKG,
        ISNULL((SELECT SUM(PalledariAmount) FROM t_WorkerAllocation wa INNER JOIN t_UnloadTransaction u ON wa.UnloadId = u.UnloadId WHERE u.RSTNumber = g.RSTNumber), 0) AS TotalPalledariCharges,
        ISNULL(l.TotalDeductionPct, 0) AS LabDeductionPct,
        g.Status
    FROM t_GateEntry g
    LEFT JOIN p02_Person p ON g.PartyId = p.PersonId
    LEFT JOIN m_Vehicle v ON g.VehicleId = v.VehicleId
    LEFT JOIN t_LabQualityCheck l ON g.RSTNumber = l.RSTNumber
    WHERE g.RSTNumber = @RSTNumber;
END
GO
