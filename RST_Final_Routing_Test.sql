/* Read-only verification for final-RST routing. No business data is changed. */
SET NOCOUNT ON;

SELECT RSTNumber, ParentRSTNumber, IsFinalRST, RSTChainStatus, Status
FROM dbo.t_GateEntry
WHERE RSTNumber = @RSTNumber;

SELECT CASE WHEN EXISTS
(
    SELECT 1 FROM dbo.t_GateEntry
    WHERE RSTNumber=@RSTNumber AND ISNULL(IsFinalRST,1)=1
)
THEN 'FINAL_RST_OK' ELSE 'INTERMEDIATE_RST_BLOCKED' END AS FinalRstGate;

SELECT CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.sp_LabItemOptions')) LIKE '%IsFinalRST%'
             THEN 'LAB_ITEM_FILTER_OK' ELSE 'LAB_ITEM_FILTER_MISSING' END AS LabItemFilter;
SELECT CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.sp_LabReportSave')) LIKE '%IsFinalRST%'
             THEN 'LAB_SAVE_FILTER_OK' ELSE 'LAB_SAVE_FILTER_MISSING' END AS LabSaveFilter;

SELECT CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.sp_LabRstList')) LIKE '%IsFinalRST%'
             THEN 'LAB_LIST_FILTER_OK' ELSE 'LAB_LIST_FILTER_MISSING' END AS LabListFilter;

-- Example safe call (read-only):
-- DECLARE @RSTNumber nvarchar(50)=N'RST-2026-000006';
-- EXEC dbo.sp_LabItemOptions @RSTNumber=@RSTNumber;
