SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetAllGateEntries
AS
BEGIN
 SET NOCOUNT ON;
 SELECT g.RSTNumber,g.InwardNo,g.VehicleId,g.PartyId,g.DriverId,
        g.GrossWeight,g.TareWeight,g.NetWeight,g.TargetOfficeId,
        COALESCE(g.GateEntryTime,h.GateInDateTime) AS GateEntryTime,g.GateExitTime,g.Status,g.StatusId,
        v.VehicleNumber,p.PersonName AS PartyName,ISNULL(p.MobileNumber,'') AS PartyMobile,d.PersonName AS DriverName,
        ISNULL(c.CategoryName,'') AS CategoryName,ISNULL(i.ItemName,'') AS ItemName,
        COALESCE(NULLIF(h.DriverMobile,''),d.MobileNumber) AS DriverMobile,
        h.ApproxNoOfBags AS TotalBags,
        ISNULL(g.WeighmentCharge,0) AS WeighmentCharge,
        CAST('Inward' AS nvarchar(20)) AS InwardOutward,
        COALESCE(u.Username,'') AS GateManName
 FROM dbo.t_GateEntry g
 LEFT JOIN dbo.m_Vehicle v ON g.VehicleId=v.VehicleId
 LEFT JOIN dbo.P02_Person p ON g.PartyId=p.PersonId
 LEFT JOIN dbo.P02_Person d ON g.DriverId=d.PersonId
 LEFT JOIN dbo.m_ItemCategory c ON g.CategoryId=c.CategoryId
 LEFT JOIN dbo.m_Item i ON g.ItemId=i.ItemId
 LEFT JOIN dbo.t_InwardHeader h ON h.InwardNo=g.InwardNo
 LEFT JOIN dbo.sa05_user u ON u.UserId=h.CreatedBy
 ORDER BY g.GateEntryTime DESC,g.RSTNumber DESC;
END;
GO
