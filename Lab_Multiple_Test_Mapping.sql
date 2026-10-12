-- Replace an item's active test selection atomically. No foreign keys or hard deletes.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabMappingSetSave
 @CategoryId int,@ItemId int,@TestIds nvarchar(max),@OriginalTestIds nvarchar(max),@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.sa05_user WHERE UserId=@UserId AND IsActive=1 AND o10_postid IN(1,5))
  THROW 50001,'Lab access is required.',1;
 IF ISJSON(@TestIds)<>1 OR LEFT(LTRIM(@TestIds),1)<>'[' OR ISJSON(@OriginalTestIds)<>1 OR LEFT(LTRIM(@OriginalTestIds),1)<>'['
  THROW 50001,'Invalid test selection.',1;
 IF EXISTS(SELECT 1 FROM OPENJSON(@TestIds) WHERE type<>2 OR TRY_CONVERT(int,value) IS NULL OR TRY_CONVERT(int,value)<=0)
  THROW 50001,'Invalid test selection.',1;
 DECLARE @Selected TABLE(TestId int PRIMARY KEY);
 INSERT @Selected SELECT DISTINCT CONVERT(int,value) FROM OPENJSON(@TestIds);
 DECLARE @Original TABLE(TestId int PRIMARY KEY);
 INSERT @Original SELECT DISTINCT TRY_CONVERT(int,value) FROM OPENJSON(@OriginalTestIds) WHERE TRY_CONVERT(int,value)>0;
 BEGIN TRY
  BEGIN TRANSACTION;
  IF NOT EXISTS(SELECT 1 FROM dbo.m_Item i WITH(UPDLOCK,HOLDLOCK) JOIN dbo.m_ItemCategory c ON c.CategoryId=i.CategoryId
     WHERE i.ItemId=@ItemId AND i.CategoryId=@CategoryId AND i.IsActive=1 AND c.IsActive=1)
   THROW 50001,'Select an active item from the selected category.',1;
  IF EXISTS(SELECT TestId FROM dbo.m_ItemLabTest WITH(UPDLOCK,HOLDLOCK) WHERE CategoryId=@CategoryId AND ItemId=@ItemId AND IsActive=1 EXCEPT SELECT TestId FROM @Original)
   OR EXISTS(SELECT TestId FROM @Original EXCEPT SELECT TestId FROM dbo.m_ItemLabTest WHERE CategoryId=@CategoryId AND ItemId=@ItemId AND IsActive=1)
   THROW 50001,'Item tests changed since this form was opened. Reload the mapping before saving.',1;
  IF EXISTS(SELECT 1 FROM @Selected s WHERE NOT EXISTS(SELECT 1 FROM dbo.m_LabTestMaster WITH(UPDLOCK,HOLDLOCK) WHERE TestId=s.TestId AND IsActive=1))
   THROW 50001,'Select active tests only. Reload the test list.',1;
  UPDATE m SET IsActive=0,ModifiedBy=@UserId,ModifiedAt=SYSDATETIME()
   FROM dbo.m_ItemLabTest m WHERE m.CategoryId=@CategoryId AND m.ItemId=@ItemId AND m.IsActive=1
   AND NOT EXISTS(SELECT 1 FROM @Selected s WHERE s.TestId=m.TestId);
  INSERT dbo.m_ItemLabTest(CategoryId,ItemId,TestId,CreatedBy)
   SELECT @CategoryId,@ItemId,s.TestId,@UserId FROM @Selected s
   WHERE NOT EXISTS(SELECT 1 FROM dbo.m_ItemLabTest m WHERE m.CategoryId=@CategoryId AND m.ItemId=@ItemId AND m.TestId=s.TestId AND m.IsActive=1);
  COMMIT;
 END TRY
 BEGIN CATCH
  IF XACT_STATE()<>0 ROLLBACK;
  THROW;
 END CATCH;
END;
GO