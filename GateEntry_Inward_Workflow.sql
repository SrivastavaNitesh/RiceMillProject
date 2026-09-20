-- Pending inward/RST workflow. No foreign keys are created.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.RstNumberSequence','SO') IS NULL
 EXEC('CREATE SEQUENCE dbo.RstNumberSequence AS bigint START WITH 1 INCREMENT BY 1');
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetPendingRstInwards
AS
BEGIN
 SET NOCOUNT ON;
 SELECT h.InwardNo,h.PartyId,COALESCE(NULLIF(h.PartyName,''),p.PersonName,'') AS PartyName,
        h.VehicleNo,h.DriverName,COALESCE(h.DriverMobile,'') AS DriverMobile
 FROM dbo.t_InwardHeader h
 JOIN dbo.m_InwardTypeMaster it ON it.InwardTypeId=h.InwardTypeId
 LEFT JOIN dbo.P02_Person p ON p.PersonId=h.PartyId
 WHERE it.RSTRequired=1 AND h.IsRSTGenerated=0 AND h.StatusId NOT IN (7,8)
 AND NOT EXISTS(SELECT 1 FROM dbo.t_GateEntry g WHERE g.InwardNo=h.InwardNo)
 ORDER BY h.InwardId DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CreateRstFromInward
 @InwardNo nvarchar(50), @GrossWeight decimal(18,2), @TargetOfficeId int=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @GrossWeight IS NULL OR @GrossWeight<=0 THROW 50001,'Enter a positive gross weight.',1;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @LockResult int;
  EXEC @LockResult=sys.sp_getapplock @Resource='GateEntry-RST-Generation',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
  IF @LockResult<0 THROW 50001,'RST generation is busy. Please retry.',1;
  DECLARE @InwardId int,@PartyId int,@VehicleNo nvarchar(50),@DriverName nvarchar(150),@Mobile nvarchar(30),@VehicleId int,@DriverId int;
  SELECT @InwardId=h.InwardId,@PartyId=h.PartyId,@VehicleNo=TRIM(h.VehicleNo),@DriverName=TRIM(h.DriverName),@Mobile=COALESCE(TRIM(h.DriverMobile),'')
  FROM dbo.t_InwardHeader h WITH(UPDLOCK,HOLDLOCK)
  JOIN dbo.m_InwardTypeMaster it ON it.InwardTypeId=h.InwardTypeId
  WHERE h.InwardNo=@InwardNo AND it.RSTRequired=1 AND h.IsRSTGenerated=0 AND h.StatusId NOT IN(7,8);
  IF @InwardId IS NULL OR EXISTS(SELECT 1 FROM dbo.t_GateEntry WITH(UPDLOCK,HOLDLOCK) WHERE InwardNo=@InwardNo)
   THROW 50001,'Select a pending RST-required inward. This inward is unavailable or already has an RST.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.P02_Person WHERE PersonId=@PartyId AND IsActive=1)
   THROW 50001,'The inward must have an active party linked before generating RST.',1;
  IF @TargetOfficeId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.o05_office WHERE OfficeId=@TargetOfficeId AND IsActive=1)
   THROW 50001,'Select an active company.',1;
  IF NULLIF(@VehicleNo,'') IS NULL OR NULLIF(@DriverName,'') IS NULL
   THROW 50001,'Vehicle and driver are required on the inward entry.',1;
  -- Match the exact inward snapshots, never an unrelated previous trip.
  IF (SELECT COUNT(*) FROM dbo.m_Vehicle WHERE UPPER(REPLACE(REPLACE(VehicleNumber,' ',''),'-',''))=UPPER(REPLACE(REPLACE(@VehicleNo,' ',''),'-','')))>1
   THROW 50001,'Duplicate vehicle masters match this inward. Correct the vehicle master first.',1;
  SELECT @VehicleId=VehicleId FROM dbo.m_Vehicle WITH(UPDLOCK,HOLDLOCK)
  WHERE UPPER(REPLACE(REPLACE(VehicleNumber,' ',''),'-',''))=UPPER(REPLACE(REPLACE(@VehicleNo,' ',''),'-','')) AND IsActive=1;
  IF @VehicleId IS NULL
  BEGIN
   IF EXISTS(SELECT 1 FROM dbo.m_Vehicle WHERE UPPER(REPLACE(REPLACE(VehicleNumber,' ',''),'-',''))=UPPER(REPLACE(REPLACE(@VehicleNo,' ',''),'-','')))
    THROW 50001,'The inward vehicle master is inactive.',1;
   INSERT dbo.m_Vehicle(VehicleNumber,IsActive) VALUES(@VehicleNo,1);
   SET @VehicleId=SCOPE_IDENTITY();
  END;
  IF (SELECT COUNT(*) FROM dbo.P02_Person WHERE LOWER(TRIM(PersonName))=LOWER(@DriverName)
      AND (@Mobile='' OR COALESCE(TRIM(MobileNumber),'')=@Mobile) AND (PersonType='Driver' OR PersonType='9'))>1
   THROW 50001,'Multiple drivers match. Add the correct mobile number on the inward entry.',1;
  SELECT @DriverId=PersonId FROM dbo.P02_Person WITH(UPDLOCK,HOLDLOCK)
  WHERE LOWER(TRIM(PersonName))=LOWER(@DriverName) AND (@Mobile='' OR COALESCE(TRIM(MobileNumber),'')=@Mobile)
    AND (PersonType='Driver' OR PersonType='9') AND IsActive=1;
  IF @DriverId IS NULL
  BEGIN
   IF EXISTS(SELECT 1 FROM dbo.P02_Person WHERE LOWER(TRIM(PersonName))=LOWER(@DriverName)
      AND (@Mobile='' OR COALESCE(TRIM(MobileNumber),'')=@Mobile) AND (PersonType='Driver' OR PersonType='9'))
    THROW 50001,'The inward driver master is inactive.',1;
   INSERT dbo.P02_Person(PersonName,MobileNumber,PersonType,IsActive) VALUES(@DriverName,NULLIF(@Mobile,''),'9',1);
   SET @DriverId=SCOPE_IDENTITY();
  END;
  DECLARE @RSTNumber nvarchar(50),@Sequence bigint;
  WHILE 1=1
  BEGIN
   SET @Sequence=NEXT VALUE FOR dbo.RstNumberSequence;
   SET @RSTNumber=CONCAT('RST-',YEAR(GETDATE()),'-',@Sequence);
   IF NOT EXISTS(SELECT 1 FROM dbo.t_GateEntry WHERE RSTNumber=@RSTNumber) BREAK;
  END;
  INSERT dbo.t_GateEntry(RSTNumber,InwardNo,VehicleId,PartyId,DriverId,GrossWeight,TargetOfficeId,Status,StatusId)
  VALUES(@RSTNumber,@InwardNo,@VehicleId,@PartyId,@DriverId,@GrossWeight,@TargetOfficeId,'Entered',2);
  UPDATE dbo.t_InwardHeader SET IsRSTGenerated=1 WHERE InwardId=@InwardId;
  COMMIT;
  SELECT @RSTNumber AS RSTNumber;
 END TRY
 BEGIN CATCH
  IF XACT_STATE()<>0 ROLLBACK;
  THROW;
 END CATCH;
END;
GO