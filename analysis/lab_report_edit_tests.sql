DECLARE @ReportId int=(SELECT MAX(ReportId) FROM dbo.t_LabReport WHERE RSTNumber='DIRECT-FULL'),
        @Version varbinary(8),@Results nvarchar(max),@Blocked bit=0,
        @ReportCount int=(SELECT COUNT(*) FROM dbo.t_LabReport),
        @ProgressCount int=(SELECT COUNT(*) FROM dbo.t_RSTProgressWork);
SELECT @Version=EditVersion FROM dbo.t_LabReport WHERE ReportId=@ReportId;
SELECT @Results=(SELECT ResultId,'23.5' Value FROM dbo.t_LabReportResult WHERE ReportId=@ReportId FOR JSON PATH);
EXEC dbo.sp_LabReportEditGet @ReportId;
EXEC dbo.sp_LabReportUpdate @ReportId,@Version,@Results,N'Corrected result',1;
IF NOT EXISTS(SELECT 1 FROM dbo.t_LabReportResult WHERE ReportId=@ReportId AND ResultValue='23.5') THROW 51000,'Edit did not update existing result.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.t_LabReport WHERE ReportId=@ReportId AND Remarks=N'Corrected result') THROW 51000,'Edit did not save remarks.',1;
IF (SELECT COUNT(*) FROM dbo.t_LabReport)<>@ReportCount OR (SELECT COUNT(*) FROM dbo.t_RSTProgressWork)<>@ProgressCount THROW 51000,'Edit duplicated report or stage.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.t_LabReportEditHistory WHERE ReportId=@ReportId AND PreviousResults LIKE '%10%' AND UpdatedResults LIKE '%23.5%') THROW 51000,'Edit audit missing.',1;
BEGIN TRY EXEC dbo.sp_LabReportUpdate @ReportId,@Version,@Results,NULL,1;
END TRY BEGIN CATCH IF ERROR_NUMBER()=50001 SET @Blocked=1; ELSE THROW; END CATCH;
IF @Blocked=0 THROW 51000,'Stale edit was accepted.',1;
SELECT @Version=EditVersion FROM dbo.t_LabReport WHERE ReportId=@ReportId;
SET @Blocked=0;
BEGIN TRY EXEC dbo.sp_LabReportUpdate @ReportId,@Version,'[{"ResultId":-1,"Value":"50"}]',NULL,1;
END TRY BEGIN CATCH IF ERROR_NUMBER()=50001 SET @Blocked=1; ELSE THROW; END CATCH;
IF @Blocked=0 THROW 51000,'Foreign result ID was accepted.',1;
SELECT @Results=(SELECT ResultId,'not numeric' Value FROM dbo.t_LabReportResult WHERE ReportId=@ReportId FOR JSON PATH);
SET @Blocked=0;
BEGIN TRY EXEC dbo.sp_LabReportUpdate @ReportId,@Version,@Results,NULL,1;
END TRY BEGIN CATCH IF ERROR_NUMBER()=50001 SET @Blocked=1; ELSE THROW; END CATCH;
IF @Blocked=0 THROW 51000,'Invalid number was accepted.',1;
IF (SELECT COUNT(*) FROM dbo.t_LabReportEditHistory WHERE ReportId=@ReportId)<>1 THROW 51000,'Rejected edit created audit.',1;
IF @@TRANCOUNT<>0 THROW 51000,'Edit leaked transaction.',1;
PRINT 'PASS: saved report update, audit, concurrency, validation and no duplicate stages/reports.';
