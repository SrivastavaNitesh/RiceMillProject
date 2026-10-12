-- Apply after Lab_RST_Stage_Workflow.sql.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO
IF COL_LENGTH('dbo.t_LabReport','EditVersion') IS NULL
    ALTER TABLE dbo.t_LabReport ADD EditVersion rowversion;
IF OBJECT_ID('dbo.t_LabReportEditHistory','U') IS NULL
    CREATE TABLE dbo.t_LabReportEditHistory
    (
        EditId bigint IDENTITY PRIMARY KEY,
        ReportId int NOT NULL,
        EditedBy int NOT NULL,
        EditedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
        PreviousRemarks nvarchar(1000) NULL,
        PreviousResults nvarchar(max) NOT NULL,
        UpdatedRemarks nvarchar(1000) NULL,
        UpdatedResults nvarchar(max) NOT NULL
    );
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabReportEditGet @ReportId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.ReportId,h.RSTNumber,h.Remarks,h.EditVersion,
           d.ResultId,d.ItemName,d.TestName,d.Unit,d.ResultValue
    FROM dbo.t_LabReport h JOIN dbo.t_LabReportResult d ON d.ReportId=h.ReportId
    WHERE h.ReportId=@ReportId
    ORDER BY d.CategoryId,d.ItemId,d.ResultId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabReportUpdate
    @ReportId int,@Version varbinary(8),@Results nvarchar(max),@Remarks nvarchar(1000)=NULL,@UserId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF ISNULL(ISJSON(@Results),0)<>1 THROW 50001,'Invalid test results.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.sa05_user u JOIN dbo.P02_Person p ON p.PersonId=u.PersonId
        WHERE u.UserId=@UserId AND u.IsActive=1 AND p.IsActive=1
        AND (u.sa10_usertypeid IN(4,5) OR p.PersonType=N'Admin' OR LOWER(REPLACE(p.PersonType,N' ',N''))=N'labtechnician'))
        THROW 50001,'An active lab technician or administrator login is required.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @RSTNumber nvarchar(50),@CurrentVersion binary(8),@PreviousRemarks nvarchar(1000);
        SELECT @RSTNumber=RSTNumber FROM dbo.t_LabReport WHERE ReportId=@ReportId;
        IF @RSTNumber IS NULL THROW 50001,'Report not found.',1;
        DECLARE @LockResult int,@LockName nvarchar(255)=N'RSTProgress:'+UPPER(LTRIM(RTRIM(@RSTNumber)));
        EXEC @LockResult=sys.sp_getapplock @Resource=@LockName,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @LockResult<0 THROW 50001,'This RST is being updated. Please retry.',1;
        SELECT @CurrentVersion=EditVersion,@PreviousRemarks=Remarks FROM dbo.t_LabReport WITH(UPDLOCK,HOLDLOCK) WHERE ReportId=@ReportId;
        IF @Version IS NULL OR @Version<>@CurrentVersion
            THROW 50001,'This report changed after you opened it. Reload the report before editing.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.t_RSTProgressWork WHERE RSTNumber=@RSTNumber AND StageId IN(7,9,13))
            THROW 50001,'Lab editing requires this RST to have reached stage 7, 9 or 13.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.t_GateEntry WHERE RSTNumber=@RSTNumber AND ISNULL(IsFinalRST,1)=1)
            THROW 50001,'Only the final RST report can be edited.',1;
        DECLARE @Input TABLE(ResultId int,Value nvarchar(max));
        INSERT @Input SELECT ResultId,Value FROM OPENJSON(@Results) WITH(ResultId int,Value nvarchar(max));
        IF NOT EXISTS(SELECT 1 FROM @Input) OR EXISTS(SELECT ResultId FROM @Input GROUP BY ResultId HAVING COUNT(*)>1)
            THROW 50001,'Fill every saved test exactly once.',1;
        IF EXISTS(SELECT 1 FROM @Input i WHERE NOT EXISTS(SELECT 1 FROM dbo.t_LabReportResult d WHERE d.ReportId=@ReportId AND d.ResultId=i.ResultId))
           OR EXISTS(SELECT 1 FROM dbo.t_LabReportResult d WHERE d.ReportId=@ReportId AND NOT EXISTS(SELECT 1 FROM @Input i WHERE i.ResultId=d.ResultId))
            THROW 50001,'Edit only the saved tests belonging to this report.',1;
        IF EXISTS(SELECT 1 FROM @Input i JOIN dbo.t_LabReportResult d ON d.ResultId=i.ResultId
            WHERE NULLIF(LTRIM(RTRIM(i.Value)),'') IS NULL OR LEN(i.Value)>500
               OR (d.ResultType='Number' AND TRY_CONVERT(decimal(18,6),i.Value) IS NULL))
            THROW 50001,'Enter a valid value for every saved test.',1;
        INSERT dbo.t_LabReportEditHistory(ReportId,EditedBy,PreviousRemarks,PreviousResults,UpdatedRemarks,UpdatedResults)
        SELECT @ReportId,@UserId,@PreviousRemarks,
            (SELECT ResultId,ResultValue FROM dbo.t_LabReportResult WHERE ReportId=@ReportId ORDER BY ResultId FOR JSON PATH),
            @Remarks,(SELECT ResultId,LTRIM(RTRIM(Value)) ResultValue FROM @Input ORDER BY ResultId FOR JSON PATH);
        UPDATE d SET ResultValue=LTRIM(RTRIM(i.Value))
        FROM dbo.t_LabReportResult d JOIN @Input i ON i.ResultId=d.ResultId WHERE d.ReportId=@ReportId;
        UPDATE dbo.t_LabReport SET Remarks=@Remarks WHERE ReportId=@ReportId;
        -- Editing values does not change test coverage or create another RST stage.
        COMMIT;
        SELECT @ReportId AS ReportId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
