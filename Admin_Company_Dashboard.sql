SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetAdminDashboard
 @OfficeId int,@FromDate date=NULL,@ToDate date=NULL
AS
BEGIN
 SET NOCOUNT ON;
 IF ISNULL(@OfficeId,0)<=0 THROW 50001,'A valid company is required.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.o05_office WHERE OfficeId=@OfficeId AND IsActive=1) THROW 50001,'Company is inactive or does not exist.',1;
 SET @ToDate=ISNULL(@ToDate,CONVERT(date,GETDATE())); SET @FromDate=ISNULL(@FromDate,DATEADD(day,-29,@ToDate));
 IF @FromDate>@ToDate OR DATEDIFF(day,@FromDate,@ToDate)>366 THROW 50001,'Invalid dashboard date range.',1;
 DECLARE @RangeEnd datetime2=DATEADD(day,1,CONVERT(datetime2,@ToDate));

 SELECT h.InwardNo,h.GateInDateTime,ISNULL(h.ApproxNoOfBags,0) ApproxNoOfBags,ISNULL(h.ApproxWeight,0) ApproxWeight,h.IsRSTGenerated
 INTO #ScopedInward FROM dbo.t_InwardHeader h JOIN dbo.m_GateMaster gm ON gm.GateId=h.GateId WHERE gm.OfficeId=@OfficeId;
 SELECT g.RSTNumber,g.InwardNo,g.VehicleId,g.PartyId,g.GateEntryTime,g.GateExitTime,g.GrossWeight,g.TareWeight,g.NetWeight,g.Status,g.StatusId,ISNULL(g.IsFinalRST,1) IsFinalRST
 INTO #ScopedRst FROM dbo.t_GateEntry g WHERE g.TargetOfficeId=@OfficeId;
 SELECT u.UnloadId,u.RSTNumber,u.UnloadTime,u.Status,sa.SupervisorActionId
 INTO #ScopedUnload FROM dbo.t_UnloadTransaction u JOIN #ScopedRst r ON r.RSTNumber=u.RSTNumber LEFT JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId AND sa.Status='Completed';
 SELECT u.RSTNumber,u.UnloadId,u.UnloadTime,sd.LocationId,sd.CategoryId,sd.ItemId,sd.BagTypeId,sd.BagCount,
 CAST(ISNULL(sd.ActualWeightKg,0) AS decimal(18,3)) WeightKg
 INTO #UnloadDetail FROM #ScopedUnload u JOIN dbo.t_SupervisorUnloadDetail sd ON sd.SupervisorActionId=u.SupervisorActionId;
 SELECT r.RSTNumber,N'Unloading' Stage,r.GateEntryTime Since INTO #Pending FROM #ScopedRst r WHERE r.GrossWeight>0 AND ISNULL(r.Status,'') NOT IN('Unloaded','Verified','Exited','Settled','Continued') AND NOT EXISTS(SELECT 1 FROM #ScopedUnload u WHERE u.RSTNumber=r.RSTNumber AND u.SupervisorActionId IS NOT NULL);
 INSERT #Pending SELECT DISTINCT u.RSTNumber,N'Lab',COALESCE(u.UnloadTime,r.GateEntryTime) FROM #ScopedUnload u JOIN #ScopedRst r ON r.RSTNumber=u.RSTNumber WHERE u.SupervisorActionId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.t_RSTProgressWork p JOIN dbo.m_RSTStageMaster s ON s.StageId=p.StageId WHERE p.RSTNumber=u.RSTNumber AND s.StageCode=N'LAB_REPORT_COMPLETED');
 INSERT #Pending SELECT r.RSTNumber,N'Billing',COALESCE((SELECT MAX(u.UnloadTime) FROM #ScopedUnload u WHERE u.RSTNumber=r.RSTNumber),r.GateEntryTime) FROM #ScopedRst r WHERE r.NetWeight>0 AND NOT EXISTS(SELECT 1 FROM dbo.t_BillingBill b WHERE b.RSTNumber=r.RSTNumber);

 DECLARE @PendingRst int=(SELECT COUNT(*) FROM #ScopedInward i WHERE ISNULL(i.IsRSTGenerated,0)=0 AND NOT EXISTS(SELECT 1 FROM #ScopedRst r WHERE r.InwardNo=i.InwardNo));
 DECLARE @ActiveRst int=(SELECT COUNT(*) FROM #ScopedRst WHERE ISNULL(Status,'') NOT IN('Exited','Settled','Continued') AND ISNULL(StatusId,0) NOT IN(7,8));
 DECLARE @FinalRst int=(SELECT COUNT(*) FROM #ScopedRst WHERE IsFinalRST=1);
 DECLARE @PendingUnload int=(SELECT COUNT(*) FROM #Pending WHERE Stage=N'Unloading'),@PendingLab int=(SELECT COUNT(*) FROM #Pending WHERE Stage=N'Lab'),@PendingBilling int=(SELECT COUNT(*) FROM #Pending WHERE Stage=N'Billing');

 SELECT @OfficeId OfficeId,o.OfficeName,GETDATE() AsOf,@FromDate FromDate,@ToDate ToDate,
 (SELECT COUNT(*) FROM #ScopedInward WHERE GateInDateTime>=@FromDate AND GateInDateTime<@RangeEnd) TodayInwardEntries,@PendingRst PendingWeighmentRst,@ActiveRst ActiveRst,@FinalRst FinalRst,
 (SELECT ISNULL(SUM(ApproxNoOfBags),0) FROM #ScopedInward WHERE GateInDateTime>=@FromDate AND GateInDateTime<@RangeEnd) TodayTotalBags,
 (SELECT ISNULL(SUM(i.ApproxWeight),0) FROM #ScopedInward i WHERE i.GateInDateTime>=@FromDate AND i.GateInDateTime<@RangeEnd) TodayTotalWeight,
 @PendingUnload PendingUnload,@PendingLab PendingLab,@PendingBilling PendingBilling
 FROM dbo.o05_office o WHERE o.OfficeId=@OfficeId;
 SELECT Name,Value FROM(VALUES(N'Pending RST',CONVERT(decimal(18,2),@PendingRst)),(N'Active RST',CONVERT(decimal(18,2),@ActiveRst)),(N'Final RST',CONVERT(decimal(18,2),@FinalRst)))s(Name,Value);
 SELECT c.CategoryId,c.CategoryName,i.ItemId,i.ItemName,b.BagTypeId,b.BagTypeName,SUM(d.BagCount) Bags
 FROM #UnloadDetail d JOIN dbo.m_ItemCategory c ON c.CategoryId=d.CategoryId JOIN dbo.m_Item i ON i.ItemId=d.ItemId JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId
 WHERE d.UnloadTime>=@FromDate AND d.UnloadTime<@RangeEnd
 GROUP BY c.CategoryId,c.CategoryName,i.ItemId,i.ItemName,b.BagTypeId,b.BagTypeName ORDER BY c.CategoryName,i.ItemName,Bags DESC,b.BagTypeName;
 SELECT l.LocationId,l.LocationName,ISNULL(SUM(d.BagCount),0) Bags,ISNULL(SUM(d.WeightKg),0) WeightKg
 FROM dbo.m_LocationMaster l JOIN dbo.t_OfficeLocationMapping om ON om.LocationId=l.LocationId AND om.OfficeId=@OfficeId AND om.IsActive=1 LEFT JOIN #UnloadDetail d ON d.LocationId=l.LocationId WHERE l.IsActive=1 GROUP BY l.LocationId,l.LocationName ORDER BY Bags DESC,l.LocationName;
 SELECT l.LocationId,l.LocationName,d.RSTNumber,c.CategoryId,c.CategoryName,i.ItemId,i.ItemName VarietyName,b.BagTypeId,b.BagTypeName,d.BagCount Bags,d.WeightKg,d.UnloadTime FROM #UnloadDetail d JOIN dbo.m_LocationMaster l ON l.LocationId=d.LocationId JOIN dbo.m_ItemCategory c ON c.CategoryId=d.CategoryId JOIN dbo.m_Item i ON i.ItemId=d.ItemId JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId WHERE d.UnloadTime>=@FromDate AND d.UnloadTime<@RangeEnd ORDER BY d.UnloadTime DESC,l.LocationName,d.RSTNumber;
 SELECT p.Stage,p.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(pr.PersonName,'') PartyName,p.Since,CAST(DATEDIFF(minute,p.Since,GETDATE())/60.0 AS decimal(10,1)) AgeHours FROM #Pending p JOIN #ScopedRst r ON r.RSTNumber=p.RSTNumber LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=r.VehicleId LEFT JOIN dbo.P02_Person pr ON pr.PersonId=r.PartyId ORDER BY AgeHours DESC,p.Stage,p.RSTNumber;

 SELECT ISNULL(NULLIF(LTRIM(RTRIM(pr.PersonName)),''),N'Unknown Supplier') SupplierName,COUNT(DISTINCT r.RSTNumber) Vehicles,ISNULL(SUM(x.Bags),0) Bags,ISNULL(SUM(r.NetWeight),0) WeightKg,
 CAST(ISNULL(AVG(NULLIF(r.NetWeight,0)),0) AS decimal(18,2)) AverageWeightKg
 FROM #ScopedRst r LEFT JOIN dbo.P02_Person pr ON pr.PersonId=r.PartyId OUTER APPLY(SELECT SUM(d.BagCount) Bags FROM #UnloadDetail d WHERE d.RSTNumber=r.RSTNumber)x
 WHERE r.GateEntryTime>=@FromDate AND r.GateEntryTime<@RangeEnd GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(pr.PersonName)),''),N'Unknown Supplier') ORDER BY WeightKg DESC,SupplierName;

 SELECT TOP(15) r.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(pr.PersonName,'') PartyName,COALESCE(stage.StageName,r.Status,N'Pending') CurrentStage,COALESCE(progress.Status,r.Status,N'Pending') CurrentStatus,
 COALESCE(progress.StartedAt,r.GateEntryTime) LastActivity,CAST(DATEDIFF(minute,COALESCE(progress.StartedAt,r.GateEntryTime),GETDATE())/60.0 AS decimal(10,1)) AgeHours
 FROM #ScopedRst r LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=r.VehicleId LEFT JOIN dbo.P02_Person pr ON pr.PersonId=r.PartyId
 OUTER APPLY(SELECT TOP(1) p.StageId,p.Status,p.StartedAt FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=r.RSTNumber ORDER BY p.ProgressId DESC)progress
 LEFT JOIN dbo.m_RSTStageMaster stage ON stage.StageId=progress.StageId WHERE ISNULL(r.Status,'') NOT IN('Exited','Settled','Continued') ORDER BY LastActivity DESC;

END;
GO
