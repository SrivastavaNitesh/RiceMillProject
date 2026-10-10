/* Optional one-time backfill from existing business records. No invented dates. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
SELECT g.RSTNumber,10,'Completed',0,g.GateEntryTime,g.GateEntryTime,g.CreatedBy,'Backfill: existing RST generated'
FROM dbo.t_GateEntry g
WHERE NULLIF(g.RSTNumber,'') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=g.RSTNumber AND p.StageId=10);

INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
SELECT u.RSTNumber,3,'Completed',0,COALESCE(u.UnloadTime,g.GateEntryTime),COALESCE(u.UnloadTime,g.GateEntryTime),u.SupervisorId,'Backfill: existing supervisor assignment'
FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
WHERE NOT EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=u.RSTNumber AND p.StageId=3);

INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
SELECT u.RSTNumber,12,'Completed',0,COALESCE(sa.CreatedAt,u.UnloadTime),COALESCE(sa.UpdatedAt,sa.CreatedAt),sa.SupervisorId,'Backfill: existing supervisor final details'
FROM dbo.t_SupervisorUnloadAction sa JOIN dbo.t_UnloadTransaction u ON u.UnloadId=sa.UnloadId
WHERE sa.Status='Completed'
  AND NOT EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=u.RSTNumber AND p.StageId=12);

INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
SELECT r.RSTNumber,6,'Completed',0,r.TestedAt,r.TestedAt,r.TestedByUserId,'Backfill: existing lab report'
FROM dbo.t_LabReport r
WHERE NOT EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=r.RSTNumber AND p.StageId=6);

INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
SELECT g.RSTNumber,7,'Completed',0,COALESCE(g.GateExitTime,g.GateEntryTime),g.GateExitTime,g.CreatedBy,'Backfill: existing tare weight'
FROM dbo.t_GateEntry g
WHERE g.TareWeight IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=g.RSTNumber AND p.StageId=7);

INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
SELECT g.RSTNumber,13,'Completed',0,g.GateExitTime,g.GateExitTime,g.CreatedBy,'Backfill: existing gate outward'
FROM dbo.t_GateEntry g
WHERE g.GateExitTime IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=g.RSTNumber AND p.StageId=13);

INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
SELECT g.RSTNumber,9,'Completed',0,g.GateExitTime,g.GateExitTime,g.CreatedBy,'Backfill: existing completed RST'
FROM dbo.t_GateEntry g
WHERE g.Status IN ('Exited','Completed')
  AND NOT EXISTS (SELECT 1 FROM dbo.t_RSTProgressWork p WHERE p.RSTNumber=g.RSTNumber AND p.StageId=9);

;WITH Latest AS (
 SELECT ProgressId,ROW_NUMBER() OVER(PARTITION BY RSTNumber ORDER BY COALESCE(CompletedAt,StartedAt) DESC,ProgressId DESC) rn
 FROM dbo.t_RSTProgressWork
)
UPDATE p SET IsActive=CASE WHEN l.rn=1 THEN 1 ELSE 0 END FROM dbo.t_RSTProgressWork p JOIN Latest l ON l.ProgressId=p.ProgressId;

MERGE dbo.t_RSTMaster AS target
USING (
 SELECT p.RSTNumber,p.StageId,p.Status,COALESCE(p.CompletedAt,p.StartedAt) UpdatedAt
 FROM dbo.t_RSTProgressWork p WHERE p.IsActive=1
) AS source ON target.RSTNumber=source.RSTNumber
WHEN MATCHED THEN UPDATE SET CurrentStageId=source.StageId,CurrentStatus=source.Status,UpdatedAt=source.UpdatedAt
WHEN NOT MATCHED THEN INSERT(RSTNumber,CurrentStageId,CurrentStatus,UpdatedAt) VALUES(source.RSTNumber,source.StageId,source.Status,source.UpdatedAt);

COMMIT;
GO
