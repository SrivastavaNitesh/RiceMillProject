CREATE OR ALTER PROCEDURE dbo.sp_MoveRSTProgress
(
    @RSTNumber NVARCHAR(50),
    @StageId INT,
    @Status NVARCHAR(50),
    @CreatedBy INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@RSTNumber)), '') IS NULL THROW 50001,'RST Number is required.',1;
    IF ISNULL(@StageId,0)<=0 THROW 50001,'Valid Stage Id is required.',1;
    IF NULLIF(LTRIM(RTRIM(@Status)), '') IS NULL THROW 50001,'Status is required.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @LockResult int,@LockName nvarchar(255)=N'RSTProgress:'+UPPER(LTRIM(RTRIM(@RSTNumber)));
        EXEC @LockResult=sys.sp_getapplock @Resource=@LockName,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @LockResult<0 THROW 50001,'This RST is being updated. Please retry.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.m_RSTStageMaster WHERE StageId=@StageId AND IsActive=1)
            THROW 50001,'The RST stage is not active or does not exist.',1;
        DECLARE @LatestProgressId bigint;
        SELECT TOP(1) @LatestProgressId=ProgressId FROM dbo.t_RSTProgressWork WITH(UPDLOCK,HOLDLOCK) WHERE RSTNumber=@RSTNumber ORDER BY ProgressId DESC;
        IF EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork WHERE ProgressId=@LatestProgressId AND StageId=@StageId AND Status=@Status AND IsActive=1)
        BEGIN
            UPDATE dbo.t_RSTMaster SET CurrentStageId=@StageId,CurrentStatus=@Status,UpdatedAt=SYSDATETIME() WHERE RSTNumber=@RSTNumber;
            IF @@ROWCOUNT=0 INSERT dbo.t_RSTMaster(RSTNumber,CurrentStageId,CurrentStatus,UpdatedAt) VALUES(@RSTNumber,@StageId,@Status,SYSDATETIME());
            COMMIT; RETURN;
        END;
        UPDATE dbo.t_RSTProgressWork SET IsActive=0,CompletedAt=COALESCE(CompletedAt,SYSDATETIME()) WHERE RSTNumber=@RSTNumber AND IsActive=1;
        INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CreatedBy) VALUES(@RSTNumber,@StageId,@Status,1,SYSDATETIME(),@CreatedBy);
        UPDATE dbo.t_RSTMaster SET CurrentStageId=@StageId,CurrentStatus=@Status,UpdatedAt=SYSDATETIME() WHERE RSTNumber=@RSTNumber;
        IF @@ROWCOUNT=0 INSERT dbo.t_RSTMaster(RSTNumber,CurrentStageId,CurrentStatus,UpdatedAt) VALUES(@RSTNumber,@StageId,@Status,SYSDATETIME());
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
