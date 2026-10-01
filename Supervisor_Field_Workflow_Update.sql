/* Safe, repeatable migration for independent Supervisor Worker/Field details. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* Multi-location assignment helper; existing sp_AssignUnloading remains unchanged. */
CREATE OR ALTER PROCEDURE dbo.sp_SaveUnloadLocations
    @UnloadId INT,
    @LocationIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @UnloadId <= 0 OR ISNULL(ISJSON(@LocationIds),0) <> 1
        THROW 50100, 'Valid unload locations are required.', 1;
    DECLARE @Locations TABLE(LocationId INT PRIMARY KEY);
    INSERT @Locations SELECT DISTINCT TRY_CONVERT(INT,[value]) FROM OPENJSON(@LocationIds) WHERE TRY_CONVERT(INT,[value]) > 0;
    IF NOT EXISTS(SELECT 1 FROM @Locations) THROW 50101, 'Select at least one unloading location.', 1;
    IF EXISTS(SELECT 1 FROM @Locations x WHERE NOT EXISTS(SELECT 1 FROM dbo.m_LocationMaster l WHERE l.LocationId=x.LocationId AND l.IsActive=1))
        THROW 50102, 'One or more unloading locations are invalid.', 1;
    BEGIN TRANSACTION;
    DELETE FROM dbo.t_UnloadLocation WHERE UnloadId=@UnloadId;
    INSERT dbo.t_UnloadLocation(UnloadId,LocationId) SELECT @UnloadId,LocationId FROM @Locations;
    COMMIT;
END;
GO

IF COL_LENGTH('dbo.t_SupervisorUnloadDetail', 'LocationId') IS NULL
    ALTER TABLE dbo.t_SupervisorUnloadDetail ADD LocationId INT NULL;
GO

IF OBJECT_ID('dbo.t_SupervisorUnloadLocationDetail', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.t_SupervisorUnloadLocationDetail
    (
        SupervisorLocationDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_t_SupervisorUnloadLocationDetail PRIMARY KEY,
        SupervisorActionId INT NOT NULL,
        LocationId INT NOT NULL,
        BagTypeId INT NOT NULL,
        BagCount INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_t_SupervisorUnloadLocationDetail_CreatedAt DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_SupervisorUnloadLocationDetail_Logical
        ON dbo.t_SupervisorUnloadLocationDetail(SupervisorActionId, LocationId, BagTypeId);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSupervisorMethWorkRegister
    @SupervisorId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UnloadId,u.RSTNumber,u.SupervisorId,u.MethId,u.LocationId,u.ItemId,u.NumberOfBags,u.Status,
           ISNULL(m.PersonName,'') AS MethName, ISNULL(loc.LocationName,'') AS LocationName,
           ISNULL(v.VehicleNumber,'') AS VehicleNumber, ISNULL(p.PersonName,'') AS PartyName,
           ISNULL(i.ItemName,'') AS ItemName,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation wa WHERE wa.UnloadId=u.UnloadId AND ISNULL(wa.BagCount,0)>0) THEN 1 ELSE 0 END AS bit) AS IsWorkerWorkCompleted,
           CAST(CASE WHEN sa.SupervisorActionId IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsSupervisorActionCompleted
    FROM dbo.t_UnloadTransaction u
    LEFT JOIN dbo.t_GateEntry ge ON ge.RSTNumber=u.RSTNumber
    LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId
    LEFT JOIN dbo.m_LocationMaster loc ON loc.LocationId=u.LocationId
    LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=ge.VehicleId
    LEFT JOIN dbo.P02_Person p ON p.PersonId=ge.PartyId
    LEFT JOIN dbo.m_Item i ON i.ItemId=u.ItemId
    LEFT JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId
    WHERE u.SupervisorId=@SupervisorId AND ISNULL(u.MethId,0)>0 AND ISNULL(u.LocationId,0)>0
    ORDER BY u.UnloadId DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveSupervisorUnloadAction
    @UnloadId INT,
    @SupervisorId INT,
    @StackPP INT=0,
    @StackJute INT=0,
    @HaudiPP INT=0,
    @HaudiJute INT=0,
    @DetailJson NVARCHAR(MAX),
    @LocationDetailJson NVARCHAR(MAX)=N'[]'
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @UnloadId<=0 OR @SupervisorId<=0 THROW 50001,'Valid unloading and supervisor are required.',1;
    DECLARE @OfficeId INT;
    SELECT @OfficeId=ge.TargetOfficeId FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry ge ON ge.RSTNumber=u.RSTNumber
    WHERE u.UnloadId=@UnloadId AND u.SupervisorId=@SupervisorId;
    IF ISNULL(@OfficeId,0)<=0 THROW 50002,'The unloading company could not be determined.',1;
    IF ISNULL(ISJSON(@DetailJson),0)<>1 OR ISNULL(ISJSON(@LocationDetailJson),0)<>1 THROW 50003,'Invalid Supervisor detail data.',1;

    DECLARE @Details TABLE(CategoryId INT,ItemId INT,LocationId INT,BagTypeId INT,BagCount INT);
    INSERT @Details SELECT CategoryId,ItemId,LocationId,BagTypeId,BagCount FROM OPENJSON(@DetailJson)
    WITH(CategoryId INT '$.CategoryId',ItemId INT '$.ItemId',LocationId INT '$.LocationId',BagTypeId INT '$.BagTypeId',BagCount INT '$.BagCount');
    IF NOT EXISTS(SELECT 1 FROM @Details) THROW 50004,'At least one material detail is required.',1;
    IF EXISTS(SELECT 1 FROM @Details WHERE CategoryId<=0 OR ItemId<=0 OR LocationId<=0 OR BagTypeId<=0 OR BagCount<=0)
        THROW 50005,'Category, item, location, bag type and bag count are required.',1;
    IF EXISTS(SELECT 1 FROM @Details d WHERE NOT EXISTS(SELECT 1 FROM dbo.m_LocationMaster l JOIN dbo.t_OfficeLocationMapping om ON om.LocationId=l.LocationId WHERE l.LocationId=d.LocationId AND om.OfficeId=@OfficeId AND l.IsActive=1 AND om.IsActive=1 AND ISNULL(om.UnloadingAllowed,1)=1))
        THROW 50006,'One or more material locations are not mapped to the company.',1;
    IF EXISTS(SELECT 1 FROM @Details d WHERE NOT EXISTS(SELECT 1 FROM dbo.m_Item i JOIN dbo.m_ItemCategory c ON c.CategoryId=i.CategoryId WHERE i.ItemId=d.ItemId AND i.CategoryId=d.CategoryId AND i.IsActive=1 AND c.IsActive=1))
        THROW 50007,'Invalid item/category selection.',1;
    IF EXISTS(SELECT 1 FROM @Details d WHERE NOT EXISTS(SELECT 1 FROM dbo.m_BagType b WHERE b.BagTypeId=d.BagTypeId AND b.IsActive=1))
        THROW 50008,'Invalid bag type selection.',1;
    IF EXISTS(SELECT CategoryId,ItemId,LocationId,BagTypeId FROM @Details GROUP BY CategoryId,ItemId,LocationId,BagTypeId HAVING COUNT(*)>1)
        THROW 50009,'Duplicate material, location and bag type combination.',1;

    DECLARE @Locations TABLE(LocationId INT,BagTypeId INT,BagCount INT);
    INSERT @Locations SELECT LocationId,BagTypeId,BagCount FROM OPENJSON(@LocationDetailJson)
    WITH(LocationId INT '$.LocationId',BagTypeId INT '$.BagTypeId',BagCount INT '$.BagCount') WHERE ISNULL(BagCount,0)>0;
    IF NOT EXISTS(SELECT 1 FROM @Locations) THROW 50010,'At least one positive location bag detail is required.',1;
    IF EXISTS(SELECT 1 FROM @Locations l WHERE NOT EXISTS(SELECT 1 FROM dbo.m_LocationMaster x JOIN dbo.t_OfficeLocationMapping om ON om.LocationId=x.LocationId WHERE x.LocationId=l.LocationId AND om.OfficeId=@OfficeId AND x.IsActive=1 AND om.IsActive=1 AND ISNULL(om.UnloadingAllowed,1)=1))
        THROW 50011,'One or more location details are not mapped to the company.',1;
    IF EXISTS(SELECT 1 FROM @Locations l WHERE NOT EXISTS(SELECT 1 FROM dbo.m_BagType b WHERE b.BagTypeId=l.BagTypeId AND b.IsActive=1))
        THROW 50012,'Invalid location bag type.',1;

    BEGIN TRANSACTION;
    DECLARE @ActionId INT;
    SELECT @ActionId=SupervisorActionId FROM dbo.t_SupervisorUnloadAction WHERE UnloadId=@UnloadId;
    IF @ActionId IS NULL
    BEGIN
        INSERT dbo.t_SupervisorUnloadAction(UnloadId,SupervisorId,MethTotalBags,StackPP,StackJute,HaudiPP,HaudiJute,SupervisorTotalBags,Status,CreatedAt)
        VALUES(@UnloadId,@SupervisorId,(SELECT ISNULL(SUM(BagCount),0) FROM dbo.t_WorkerAllocation WHERE UnloadId=@UnloadId),@StackPP,@StackJute,@HaudiPP,@HaudiJute,(SELECT SUM(BagCount) FROM @Details),'Completed',SYSDATETIME());
        SET @ActionId=SCOPE_IDENTITY();
    END
    ELSE
        UPDATE dbo.t_SupervisorUnloadAction SET UpdatedAt=SYSDATETIME(),SupervisorTotalBags=(SELECT SUM(BagCount) FROM @Details),Status='Completed' WHERE SupervisorActionId=@ActionId;
    DELETE FROM dbo.t_SupervisorUnloadDetail WHERE SupervisorActionId=@ActionId;
    INSERT dbo.t_SupervisorUnloadDetail(SupervisorActionId,CategoryId,ItemId,LocationId,BagTypeId,BagCount,CreatedAt)
    SELECT @ActionId,CategoryId,ItemId,LocationId,BagTypeId,BagCount,SYSDATETIME() FROM @Details;
    DELETE FROM dbo.t_SupervisorUnloadLocationDetail WHERE SupervisorActionId=@ActionId;
    INSERT dbo.t_SupervisorUnloadLocationDetail(SupervisorActionId,LocationId,BagTypeId,BagCount)
    SELECT @ActionId,LocationId,BagTypeId,BagCount FROM @Locations;
    COMMIT;
    SELECT @ActionId AS SupervisorActionId;
END;
GO
