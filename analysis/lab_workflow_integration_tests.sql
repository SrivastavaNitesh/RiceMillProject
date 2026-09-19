-- Run only on the disposable database created by lab_test_database_setup.sql.
IF DB_NAME()<>'RiceMillLab_Test_01a0abba' THROW 51000,'Wrong database for synthetic tests.',1;
SET NOCOUNT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
GO
EXEC dbo.sp_LabTestSave @TestName=N'Moisture',@Unit=N'%',@ResultType='Number',@UserId=1;
EXEC dbo.sp_LabTestSave @TestName=N'Observation',@ResultType='Text',@UserId=1;
EXEC dbo.sp_LabMappingSave @CategoryId=1,@ItemId=1,@TestId=1,@UserId=1;
EXEC dbo.sp_LabMappingSave @CategoryId=1,@ItemId=1,@TestId=2,@UserId=1;
EXEC dbo.sp_LabMappingSave @CategoryId=1,@ItemId=2,@TestId=1,@UserId=1;
EXEC dbo.sp_LabMappingSave @CategoryId=2,@ItemId=3,@TestId=2,@UserId=1;
EXEC dbo.sp_CompleteUnloadWithItems @UnloadId=1,@GateManId=4,@BagTypeId=1,@NumberOfBags=20,@LocationId=1,@Shift='Day',@ItemIds='[1,2,3]',@WorkerIds='[3]';
EXEC dbo.sp_CompleteUnloadWithItems @UnloadId=2,@GateManId=4,@BagTypeId=1,@NumberOfBags=10,@LocationId=1,@Shift='Night',@ItemIds='[4]',@WorkerIds='[]';
IF (SELECT COUNT(*) FROM dbo.t_UnloadItem WHERE UnloadId=1)<>3 THROW 51000,'Unloaded items not saved.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys)<>0 THROW 51000,'Unexpected relationship constraint.',1;
PRINT 'PASS: multi-category unloaded items saved, zero foreign keys';
GO
DECLARE @Rejected bit=0;
BEGIN TRY
 EXEC dbo.sp_CompleteUnloadWithItems @UnloadId=1,@GateManId=4,@BagTypeId=1,@NumberOfBags=20,@LocationId=1,@Shift='Day',@ItemIds='[1]',@WorkerIds='[3]';
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 OR (SELECT COUNT(*) FROM dbo.t_WorkerAllocation WHERE UnloadId=1)<>1 THROW 51000,'Unloading replay was not rejected.',1;
PRINT 'PASS: unloading replay rejected without duplicate workers';
GO
DECLARE @Rejected bit=0;
BEGIN TRY EXEC dbo.sp_LabMappingSave @CategoryId=2,@ItemId=1,@TestId=1,@UserId=1;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 THROW 51000,'Cross-category mapping accepted.',1;
PRINT 'PASS: cross-category mapping rejected';
GO
DECLARE @Rejected bit=0;
BEGIN TRY EXEC dbo.sp_LabMappingSave @CategoryId=1,@ItemId=1,@TestId=1,@UserId=1;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 THROW 51000,'Duplicate mapping accepted.',1;
PRINT 'PASS: duplicate mapping rejected';
GO
DECLARE @Rejected bit=0,@Token uniqueidentifier=NEWID();
BEGIN TRY EXEC dbo.sp_LabReportSave @RSTNumber='LAB-TEST-A',@SubmissionId=@Token,@SelectedItems='[4]',@Results='[]',@UserId=1;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 THROW 51000,'Item from another RST accepted.',1;
PRINT 'PASS: item belonging only to another RST rejected';
GO
DECLARE @Rejected bit=0,@Token uniqueidentifier=NEWID();
BEGIN TRY EXEC dbo.sp_LabReportSave @RSTNumber='LAB-TEST-A',@SubmissionId=@Token,@SelectedItems='[1]',@Results='[{"CategoryId":1,"ItemId":1,"TestId":1,"Value":"12"}]',@UserId=1;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 THROW 51000,'Incomplete test list accepted.',1;
PRINT 'PASS: missing mapped result rejected';
GO
DECLARE @Rejected bit=0,@Token uniqueidentifier=NEWID();
BEGIN TRY EXEC dbo.sp_LabReportSave @RSTNumber='LAB-TEST-A',@SubmissionId=@Token,@SelectedItems='[1]',@Results='[{"CategoryId":1,"ItemId":1,"TestId":1,"Value":"abc"},{"CategoryId":1,"ItemId":1,"TestId":2,"Value":"Good"}]',@UserId=1;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 THROW 51000,'Non-numeric number result accepted.',1;
PRINT 'PASS: numeric result validation';
GO
DECLARE @Rejected bit=0,@Token uniqueidentifier=NEWID();
BEGIN TRY EXEC dbo.sp_LabReportSave @RSTNumber='LAB-TEST-A',@SubmissionId=@Token,@SelectedItems='[1]',@Results='[{"CategoryId":1,"ItemId":1,"TestId":1,"Value":"12"},{"CategoryId":1,"ItemId":1,"TestId":1,"Value":"13"}]',@UserId=1;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 THROW 51000,'Duplicate test result accepted.',1;
PRINT 'PASS: duplicate test result rejected';
GO
DECLARE @Rejected bit=0;
BEGIN TRY EXEC dbo.sp_LabTestSave @TestName=N'Unauthorized test',@ResultType='Text',@UserId=3;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; IF ERROR_NUMBER()=50001 SET @Rejected=1; ELSE THROW; END CATCH;
IF @Rejected=0 THROW 51000,'Non-lab user allowed.',1;
PRINT 'PASS: non-lab user rejected';
GO
DECLARE @Token uniqueidentifier='00000000-0000-0000-0000-000000000101';
EXEC dbo.sp_LabReportSave @RSTNumber='LAB-TEST-A',@SubmissionId=@Token,@SelectedItems='[1,3]',@Results='[{"CategoryId":1,"ItemId":1,"TestId":1,"Value":"12.5"},{"CategoryId":1,"ItemId":1,"TestId":2,"Value":"Good"},{"CategoryId":2,"ItemId":3,"TestId":2,"Value":"Clean"}]',@UserId=1,@Remarks='Integration test';
EXEC dbo.sp_LabReportSave @RSTNumber='LAB-TEST-A',@SubmissionId=@Token,@SelectedItems='[1,3]',@Results='[]',@UserId=1;
IF (SELECT COUNT(*) FROM dbo.t_LabReport)<>1 OR (SELECT COUNT(*) FROM dbo.t_LabReportResult)<>3 THROW 51000,'Atomic/idempotent report save failed.',1;
PRINT 'PASS: multiple categories/items in one report, repeat token creates no duplicate';
GO
EXEC dbo.sp_LabTestSave @TestId=1,@TestName=N'Moisture revised',@Unit='%',@ResultType='Number',@UserId=1;
EXEC dbo.sp_LabTestRemove @TestId=2,@UserId=1;
IF NOT EXISTS(SELECT 1 FROM dbo.t_LabReportResult WHERE TestName='Moisture' AND ResultValue='12.5') OR (SELECT COUNT(*) FROM dbo.t_LabReportResult WHERE TestName='Observation')<>2 THROW 51000,'Historic result snapshots changed.',1;
IF EXISTS(SELECT 1 FROM dbo.m_ItemLabTest WHERE TestId=2 AND IsActive=1) THROW 51000,'Removed test still has active mappings.',1;
PRINT 'PASS: test edit/remove preserves historical reports and deactivates mappings';
EXEC dbo.sp_LabMappingSave @MappingId=3,@CategoryId=2,@ItemId=3,@TestId=1,@UserId=1;
IF NOT EXISTS(SELECT 1 FROM dbo.m_ItemLabTest WHERE MappingId=3 AND CategoryId=2 AND ItemId=3) THROW 51000,'Mapping edit failed.',1;
EXEC dbo.sp_LabMappingRemove @MappingId=3,@UserId=1;
IF EXISTS(SELECT 1 FROM dbo.m_ItemLabTest WHERE MappingId=3 AND IsActive=1) THROW 51000,'Mapping remove failed.',1;
PRINT 'PASS: mapping edit/remove';
-- Restore active mappings for browser verification, using new master IDs after removal.
EXEC dbo.sp_LabTestSave @TestName=N'Observation',@ResultType='Text',@UserId=1;
EXEC dbo.sp_LabMappingSave @CategoryId=1,@ItemId=1,@TestId=3,@UserId=1;
EXEC dbo.sp_LabMappingSave @CategoryId=2,@ItemId=3,@TestId=3,@UserId=1;
EXEC dbo.sp_LabMappingSave @CategoryId=1,@ItemId=2,@TestId=1,@UserId=1;
PRINT 'ALL INTEGRATION ASSERTIONS PASSED';
GO
