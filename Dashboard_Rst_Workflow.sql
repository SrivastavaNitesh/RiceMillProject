-- Dashboard/register compatibility for inward-linked RST records. No foreign keys.
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
        v.VehicleNumber,p.PersonName AS PartyName,d.PersonName AS DriverName,
        COALESCE(NULLIF(h.DriverMobile,''),d.MobileNumber) AS DriverMobile,
        h.ApproxNoOfBags AS TotalBags,
        CAST(NULL AS decimal(18,2)) AS WeighmentCharge,
        CAST('Inward' AS nvarchar(20)) AS InwardOutward,
        COALESCE(u.Username,'') AS GateManName
 FROM dbo.t_GateEntry g
 LEFT JOIN dbo.m_Vehicle v ON g.VehicleId=v.VehicleId
 LEFT JOIN dbo.P02_Person p ON g.PartyId=p.PersonId
 LEFT JOIN dbo.P02_Person d ON g.DriverId=d.PersonId
 LEFT JOIN dbo.t_InwardHeader h ON h.InwardNo=g.InwardNo
 LEFT JOIN dbo.sa05_user u ON u.UserId=h.CreatedBy
 ORDER BY g.GateEntryTime DESC,g.RSTNumber DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardStats
AS
BEGIN
 SET NOCOUNT ON;
 -- Count an inward once even after its RST is generated; retain legacy RSTs without inward headers.
 DECLARE @TotalEntered int=(SELECT COUNT(*) FROM dbo.t_InwardHeader)
   +(SELECT COUNT(*) FROM dbo.t_GateEntry g WHERE NOT EXISTS(SELECT 1 FROM dbo.t_InwardHeader h WHERE h.InwardNo=g.InwardNo));
 DECLARE @PendingUnload int=(SELECT COUNT(*) FROM dbo.t_GateEntry g
   WHERE g.GateExitTime IS NULL AND ISNULL(g.StatusId,1) NOT IN(7,8) AND ISNULL(g.Status,'') NOT IN('Exited','Settled')
   AND (NOT EXISTS(SELECT 1 FROM dbo.t_UnloadTransaction u WHERE u.RSTNumber=g.RSTNumber)
     OR EXISTS(SELECT 1 FROM dbo.t_UnloadTransaction u WHERE u.RSTNumber=g.RSTNumber AND u.Status='Assigned')));
 -- Use the same current test coverage as the Lab dashboard, including items needing mappings.
 DECLARE @Lab TABLE(RSTNumber nvarchar(50),VehicleNumber nvarchar(50),PartyName nvarchar(250),ItemCount int,RequiredTests int,CompletedTests int,UnmappedItems int);
 INSERT @Lab EXEC dbo.sp_LabRstList;
 DECLARE @PendingLab int=(SELECT COUNT(*) FROM @Lab WHERE CompletedTests<RequiredTests OR UnmappedItems>0);
 DECLARE @PendingSettlement int=(SELECT COUNT(*) FROM dbo.t_GateEntry WHERE NetWeight>0 AND ISNULL(StatusId,1)<>8 AND ISNULL(Status,'')<>'Settled');
 SELECT @TotalEntered AS TotalEntered,@PendingUnload AS PendingUnload,@PendingLab AS PendingLab,@PendingSettlement AS PendingSettlement;
END;
GO