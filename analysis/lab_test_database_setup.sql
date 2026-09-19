-- Disposable integration-test database. Do not change this target to the application database.
USE master;
GO
IF DB_ID('RiceMillLab_Test_01a0abba') IS NOT NULL THROW 51000,'Test database already exists; inspect it before reuse.',1;
CREATE DATABASE RiceMillLab_Test_01a0abba;
GO
USE RiceMillLab_Test_01a0abba;
GO
SELECT TOP(0) * INTO dbo.m_Item FROM RiceMillDB.dbo.m_Item;
SELECT TOP(0) * INTO dbo.m_ItemCategory FROM RiceMillDB.dbo.m_ItemCategory;
SELECT TOP(0) * INTO dbo.m_BagType FROM RiceMillDB.dbo.m_BagType;
SELECT TOP(0) * INTO dbo.m_Vehicle FROM RiceMillDB.dbo.m_Vehicle;
SELECT TOP(0) * INTO dbo.P02_Person FROM RiceMillDB.dbo.P02_Person;
SELECT TOP(0) * INTO dbo.sa05_user FROM RiceMillDB.dbo.sa05_user;
SELECT TOP(0) * INTO dbo.sa10_usertype FROM RiceMillDB.dbo.sa10_usertype;
SELECT TOP(0) * INTO dbo.o10_post FROM RiceMillDB.dbo.o10_post;
SELECT TOP(0) * INTO dbo.o_OfficeLocation FROM RiceMillDB.dbo.o_OfficeLocation;
SELECT TOP(0) * INTO dbo.t_GateEntry FROM RiceMillDB.dbo.t_GateEntry;
SELECT TOP(0) * INTO dbo.t_UnloadTransaction FROM RiceMillDB.dbo.t_UnloadTransaction;
SELECT TOP(0) * INTO dbo.t_UnloadLocation FROM RiceMillDB.dbo.t_UnloadLocation;
SELECT TOP(0) * INTO dbo.t_WorkerAllocation FROM RiceMillDB.dbo.t_WorkerAllocation;
GO
INSERT dbo.m_ItemCategory(CategoryName,IsActive) VALUES('Paddy',1),('Packing',1),('Not unloaded category',1);
INSERT dbo.m_Item(ItemName,CategoryId,Unit,IsActive) VALUES('Paddy A',1,'Kg',1),('Paddy B',1,'Kg',1),('Bags',2,'Pcs',1),('Not unloaded item',3,'Kg',1);
INSERT dbo.m_BagType(BagTypeName,DeductionWeightGrams,IsActive) VALUES('Test bag',100,1);
INSERT dbo.m_Vehicle(VehicleNumber,IsActive) VALUES('TEST-VEHICLE',1);
INSERT dbo.P02_Person(PersonName,PersonType,IsActive) VALUES('Test Lab Technician','Lab Technician',1),('Test Admin','Admin',1),('Test Worker','Worker',1),('Test Gate Man','Gate Man',1);
INSERT dbo.sa05_user(Username,PasswordHash,PersonId,o10_postid,sa10_usertypeid,IsActive) VALUES('lab-test','LabTest-Only-2026',1,5,5,1),('admin-test','LabTest-Only-2026',2,1,1,1),('worker-test','LabTest-Only-2026',3,7,7,1);
INSERT dbo.sa10_usertype(UserTypeId,UserTypeName,LayoutName,IsActive) VALUES(5,'Lab Technician','_LayoutLab',1),(1,'Admin','_LayoutAdmin',1),(7,'Worker','_Layout',1);
INSERT dbo.o_OfficeLocation(OfficeId,LocationName,IsActive) VALUES(1,'Test Godown',1);
INSERT dbo.t_GateEntry(RSTNumber,VehicleId,PartyId,DriverId,GrossWeight,GateEntryTime,Status,StatusId) VALUES('LAB-TEST-A',1,2,4,1000,GETDATE(),'Entered',1),('LAB-TEST-B',1,2,4,2000,GETDATE(),'Entered',1);
INSERT dbo.t_UnloadTransaction(RSTNumber,LocationId,SupervisorId,MethId,Status) VALUES('LAB-TEST-A',1,2,2,'Assigned'),('LAB-TEST-B',1,2,2,'Assigned');
GO
CREATE PROCEDURE dbo.sp_ValidateUser @Username nvarchar(100),@Password nvarchar(256)
AS SELECT u.UserId,u.Username,u.PersonId,p.PersonName,u.o10_postid,u.sa10_usertypeid,p.PersonType AS Role,t.LayoutName
FROM dbo.sa05_user u JOIN dbo.P02_Person p ON p.PersonId=u.PersonId JOIN dbo.sa10_usertype t ON t.UserTypeId=u.sa10_usertypeid
WHERE u.Username=@Username AND u.PasswordHash=@Password AND u.IsActive=1;
GO
