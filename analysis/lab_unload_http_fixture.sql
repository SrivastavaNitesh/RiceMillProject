IF DB_NAME()<>'RiceMillLab_Test_01a0abba' THROW 51000,'Wrong database for synthetic fixture.',1;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetAllBagTypes
AS SELECT BagTypeId,BagTypeName,DeductionWeightGrams,IsActive FROM dbo.m_BagType WHERE IsActive=1;
GO
IF NOT EXISTS(SELECT 1 FROM dbo.t_GateEntry WHERE RSTNumber='LAB-TEST-C')
BEGIN
 INSERT dbo.t_GateEntry(RSTNumber,VehicleId,PartyId,DriverId,GrossWeight,GateEntryTime,Status,StatusId) VALUES('LAB-TEST-C',1,2,4,1000,GETDATE(),'Entered',1);
 INSERT dbo.t_UnloadTransaction(RSTNumber,LocationId,SupervisorId,MethId,Status) VALUES('LAB-TEST-C',NULL,2,2,'Assigned');
END;
SELECT UnloadId FROM dbo.t_UnloadTransaction WHERE RSTNumber='LAB-TEST-C';
