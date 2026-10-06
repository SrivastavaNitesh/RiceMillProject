CREATE OR ALTER PROCEDURE dbo.sp_ManageLocationMaster
    @Action VARCHAR(20), @LocationId INT=NULL, @LocationCode NVARCHAR(20)=NULL OUTPUT,
    @LocationName NVARCHAR(100)=NULL, @LocationType NVARCHAR(50)=NULL,
    @Capacity DECIMAL(18,2)=NULL, @CapacityUnit NVARCHAR(10)=NULL,
    @Description NVARCHAR(250)=NULL, @Address NVARCHAR(250)=NULL,
    @Remarks NVARCHAR(250)=NULL, @IsActive BIT=1, @CreatedBy INT=1, @OfficeId INT=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @Action='INSERT'
    BEGIN
        IF ISNULL(@OfficeId,0)=0 THROW 50001,'Office selection is required.',1;
        BEGIN TRANSACTION;
        DECLARE @NextId INT, @Suffix INT;
        SELECT @NextId=ISNULL(MAX(LocationId),0)+1 FROM dbo.m_LocationMaster WITH (UPDLOCK,HOLDLOCK);
        SELECT @Suffix=ISNULL(MAX(TRY_CONVERT(INT,SUBSTRING(LocationCode,5,20))),0)+1 FROM dbo.m_LocationMaster WITH (UPDLOCK,HOLDLOCK);
        IF @Suffix<@NextId SET @Suffix=@NextId;
        SET @LocationCode='LOC-'+CONVERT(VARCHAR(15),@Suffix);
        WHILE EXISTS(SELECT 1 FROM dbo.m_LocationMaster WITH (UPDLOCK,HOLDLOCK) WHERE LocationCode=@LocationCode)
        BEGIN SET @Suffix=@Suffix+1; SET @LocationCode='LOC-'+CONVERT(VARCHAR(15),@Suffix); END;
        INSERT dbo.m_LocationMaster(LocationCode,LocationName,LocationType,Capacity,CapacityUnit,Description,Address,Remarks,IsActive,CreatedBy,CreatedDate)
        VALUES(@LocationCode,@LocationName,@LocationType,ISNULL(@Capacity,0),@CapacityUnit,@Description,@Address,@Remarks,ISNULL(@IsActive,1),@CreatedBy,GETDATE());
        SET @LocationId=CONVERT(INT,SCOPE_IDENTITY());
        INSERT dbo.t_OfficeLocationMapping(OfficeId,LocationId,UnloadingAllowed,EffectiveFrom,IsActive,CreatedBy,CreatedDate)
        VALUES(@OfficeId,@LocationId,1,CONVERT(date,GETDATE()),1,@CreatedBy,GETDATE());
        COMMIT; SELECT @LocationId LocationId,@LocationCode LocationCode; RETURN;
    END;
    IF @Action='UPDATE'
    BEGIN
        IF ISNULL(@OfficeId,0)=0 THROW 50001,'Office selection is required.',1;
        UPDATE dbo.m_LocationMaster SET LocationName=@LocationName,LocationType=@LocationType,Capacity=ISNULL(@Capacity,0),CapacityUnit=@CapacityUnit,Description=@Description,Address=@Address,Remarks=@Remarks,IsActive=@IsActive,ModifiedBy=@CreatedBy,ModifiedDate=GETDATE() WHERE LocationId=@LocationId;
        UPDATE dbo.t_OfficeLocationMapping SET IsActive=0,ModifiedBy=@CreatedBy,ModifiedDate=GETDATE() WHERE LocationId=@LocationId AND OfficeId<>@OfficeId AND IsActive=1;
        IF EXISTS(SELECT 1 FROM dbo.t_OfficeLocationMapping WHERE LocationId=@LocationId AND OfficeId=@OfficeId) UPDATE dbo.t_OfficeLocationMapping SET IsActive=1,ModifiedBy=@CreatedBy,ModifiedDate=GETDATE() WHERE LocationId=@LocationId AND OfficeId=@OfficeId;
        ELSE INSERT dbo.t_OfficeLocationMapping(OfficeId,LocationId,UnloadingAllowed,EffectiveFrom,IsActive,CreatedBy,CreatedDate) VALUES(@OfficeId,@LocationId,1,CONVERT(date,GETDATE()),1,@CreatedBy,GETDATE()); RETURN;
    END;
    IF @Action IN ('SELECT_ALL','SELECT_BY_ID')
    BEGIN
        SELECT l.*,map.OfficeId,o.OfficeName FROM dbo.m_LocationMaster l OUTER APPLY(SELECT TOP(1) m.OfficeId FROM dbo.t_OfficeLocationMapping m WHERE m.LocationId=l.LocationId AND m.IsActive=1 ORDER BY m.MappingId DESC) map LEFT JOIN dbo.o05_office o ON o.OfficeId=map.OfficeId WHERE @Action='SELECT_ALL' OR l.LocationId=@LocationId ORDER BY l.LocationId DESC; RETURN;
    END;
    IF @Action='DELETE' BEGIN UPDATE dbo.m_LocationMaster SET IsActive=0 WHERE LocationId=@LocationId; UPDATE dbo.t_OfficeLocationMapping SET IsActive=0 WHERE LocationId=@LocationId; END;
END;
GO
