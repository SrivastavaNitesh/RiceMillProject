SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ManageLocationMaster
 @Action VARCHAR(20),@LocationId INT=NULL,@LocationCode NVARCHAR(20)=NULL OUTPUT,@LocationName NVARCHAR(100)=NULL,
 @LocationType NVARCHAR(50)=NULL,@Capacity DECIMAL(18,2)=NULL,@CapacityUnit NVARCHAR(10)=NULL,@Description NVARCHAR(250)=NULL,
 @Address NVARCHAR(250)=NULL,@Remarks NVARCHAR(250)=NULL,@IsActive BIT=1,@CreatedBy INT=1,@OfficeId INT=NULL,@SubCompanyId INT=NULL
AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @Action IN('INSERT','UPDATE')
 BEGIN
  IF ISNULL(@OfficeId,0)=0 THROW 50001,'Company selection is required.',1;
  IF ISNULL(@SubCompanyId,0)>0 AND NOT EXISTS(SELECT 1 FROM dbo.tbl_subcompany s JOIN dbo.tbl_company c ON c.CompanyId=s.CompanyId WHERE s.SubCompanyId=@SubCompanyId AND s.IsActive=1 AND c.IsActive=1 AND c.OfficeId=@OfficeId)
   THROW 50003,'Selected subcompany does not belong to the selected company.',1;
 END;
 IF @Action='INSERT'
 BEGIN
  BEGIN TRAN;DECLARE @NextId INT,@Suffix INT;
  SELECT @NextId=ISNULL(MAX(LocationId),0)+1 FROM dbo.m_LocationMaster WITH(UPDLOCK,HOLDLOCK);
  SELECT @Suffix=ISNULL(MAX(TRY_CONVERT(INT,SUBSTRING(LocationCode,5,20))),0)+1 FROM dbo.m_LocationMaster WITH(UPDLOCK,HOLDLOCK);
  IF @Suffix<@NextId SET @Suffix=@NextId;SET @LocationCode='LOC-'+CONVERT(VARCHAR(15),@Suffix);
  WHILE EXISTS(SELECT 1 FROM dbo.m_LocationMaster WITH(UPDLOCK,HOLDLOCK) WHERE LocationCode=@LocationCode) BEGIN SET @Suffix+=1;SET @LocationCode='LOC-'+CONVERT(VARCHAR(15),@Suffix);END;
  INSERT dbo.m_LocationMaster(LocationCode,LocationName,LocationType,Capacity,CapacityUnit,Description,Address,Remarks,IsActive,CreatedBy,CreatedDate,SubCompanyId)
  VALUES(@LocationCode,@LocationName,@LocationType,ISNULL(@Capacity,0),@CapacityUnit,@Description,@Address,@Remarks,ISNULL(@IsActive,1),@CreatedBy,GETDATE(),@SubCompanyId);
  SET @LocationId=CONVERT(INT,SCOPE_IDENTITY());
  INSERT dbo.t_OfficeLocationMapping(OfficeId,LocationId,UnloadingAllowed,EffectiveFrom,IsActive,CreatedBy,CreatedDate) VALUES(@OfficeId,@LocationId,1,CONVERT(DATE,GETDATE()),1,@CreatedBy,GETDATE());
  COMMIT;SELECT @LocationId LocationId,@LocationCode LocationCode;RETURN;
 END;
 IF @Action='UPDATE'
 BEGIN
  UPDATE dbo.m_LocationMaster SET LocationName=@LocationName,LocationType=@LocationType,Capacity=ISNULL(@Capacity,0),CapacityUnit=@CapacityUnit,Description=@Description,Address=@Address,Remarks=@Remarks,IsActive=@IsActive,SubCompanyId=@SubCompanyId,ModifiedBy=@CreatedBy,ModifiedDate=GETDATE() WHERE LocationId=@LocationId;
  UPDATE dbo.t_OfficeLocationMapping SET IsActive=0,ModifiedBy=@CreatedBy,ModifiedDate=GETDATE() WHERE LocationId=@LocationId AND OfficeId<>@OfficeId AND IsActive=1;
  IF EXISTS(SELECT 1 FROM dbo.t_OfficeLocationMapping WHERE LocationId=@LocationId AND OfficeId=@OfficeId) UPDATE dbo.t_OfficeLocationMapping SET IsActive=1,ModifiedBy=@CreatedBy,ModifiedDate=GETDATE() WHERE LocationId=@LocationId AND OfficeId=@OfficeId;
  ELSE INSERT dbo.t_OfficeLocationMapping(OfficeId,LocationId,UnloadingAllowed,EffectiveFrom,IsActive,CreatedBy,CreatedDate) VALUES(@OfficeId,@LocationId,1,CONVERT(DATE,GETDATE()),1,@CreatedBy,GETDATE());RETURN;
 END;
 IF @Action IN('SELECT_ALL','SELECT_BY_ID')
 BEGIN
  SELECT l.*,m.OfficeId,o.OfficeName,c.CompanyId,s.SubCompanyName,c.CompanyName FROM dbo.m_LocationMaster l
  OUTER APPLY(SELECT TOP(1) x.OfficeId FROM dbo.t_OfficeLocationMapping x WHERE x.LocationId=l.LocationId AND x.IsActive=1 ORDER BY x.MappingId DESC)m
  LEFT JOIN dbo.o05_office o ON o.OfficeId=m.OfficeId LEFT JOIN dbo.tbl_subcompany s ON s.SubCompanyId=l.SubCompanyId
  LEFT JOIN dbo.tbl_company c ON c.CompanyId=s.CompanyId OR(s.SubCompanyId IS NULL AND c.OfficeId=m.OfficeId)
  WHERE @Action='SELECT_ALL' OR l.LocationId=@LocationId ORDER BY l.LocationId DESC;RETURN;
 END;
 IF @Action='DELETE' BEGIN UPDATE dbo.m_LocationMaster SET IsActive=0 WHERE LocationId=@LocationId;UPDATE dbo.t_OfficeLocationMapping SET IsActive=0 WHERE LocationId=@LocationId;END;
END;
GO
