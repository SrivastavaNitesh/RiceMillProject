-- Preserve all master records; optional deduction units must not hide tests.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTestList
AS
BEGIN
 SET NOCOUNT ON;
 SELECT t.TestId,t.TestName,t.Unit,t.ResultType,t.Description,t.IsActive,
        t.Deducations,t.Deducationsunitid AS UnitId,u.UnitName
 FROM dbo.m_LabTestMaster t
 LEFT JOIN dbo.tbl_Masterofunit u ON u.UnitId=t.Deducationsunitid
 WHERE t.IsActive=1
 ORDER BY t.TestName;
END;
GO