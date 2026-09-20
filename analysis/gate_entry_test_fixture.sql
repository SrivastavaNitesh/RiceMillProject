IF DB_NAME()<>'RiceMillLab_Test_01a0abba' THROW 51000,'Synthetic database only',1;
SET NOCOUNT ON;
IF NOT EXISTS(SELECT 1 FROM m_InwardTypeMaster)
 INSERT m_InwardTypeMaster(InwardTypeName,RSTRequired,IsActive) VALUES('With RST',1,1),('Without RST',0,1);
IF NOT EXISTS(SELECT 1 FROM o05_office)
 INSERT o05_office(OfficeName,Location,IsActive) VALUES('Test Company','Test address',1);
IF NOT EXISTS(SELECT 1 FROM t_InwardHeader WHERE InwardNo='RST-UI-A')
 INSERT t_InwardHeader(GateEntryNo,InwardNo,InwardDate,InwardTime,GateId,InwardTypeId,PartyId,VehicleNo,VehicleTypeId,DriverName,DriverMobile,GateInDateTime,StatusId,IsRSTGenerated,CreatedDate)
 VALUES('TEST-A','RST-UI-A',GETDATE(),'10:00',1,1,2,'TEST-VEHICLE-A',1,'Test Driver A','1111111111',GETDATE(),1,0,GETDATE()),
 ('TEST-B','RST-UI-B',GETDATE(),'10:00',1,1,2,'TEST-VEHICLE-B',1,'Test Driver B','2222222222',GETDATE(),1,0,GETDATE()),
 ('TEST-C','RST-UI-C',GETDATE(),'10:00',1,1,2,'TEST-VEHICLE-B',1,'Test Driver C','3333333333',GETDATE(),3,0,GETDATE()),
 ('TEST-N','RST-NOT-REQUIRED',GETDATE(),'10:00',1,2,2,'TEST-NO-RST',1,'Not RST','',GETDATE(),1,0,GETDATE()),
 ('TEST-X','RST-EXITED',GETDATE(),'10:00',1,1,2,'TEST-EXITED',1,'Exited','',GETDATE(),7,0,GETDATE());
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetAllOffices AS SELECT OfficeId,OfficeName,Location,IsActive FROM dbo.o05_office;
GO
EXEC sp_GetPendingRstInwards;