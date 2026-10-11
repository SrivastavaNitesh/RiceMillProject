# Lab RST stage workflow

Applied to the configured database on 2026-10-10. Stage 7 remains TARE_WEIGHT;
new stage codes resolve to 14 (LAB_REPORT_PARTIAL) and 15 (LAB_REPORT_COMPLETED).
The migration resolves new IDs by StageCode rather than assuming these numbers.

New reports require a final RST that has reached stage 7, 9 or 13 in its progress
history, even when its current stage has subsequently changed. Stage 9/13 does not require prior stage 7. Items come from completed supervisor final
details consistently across listing, item selection, validation and coverage.
Every accepted report appends a progress row, including consecutive partial
reports. Unmapped items prevent full completion. Reports, progress and the
t_RSTMaster current-stage cache commit in one transaction. Same-submission
retries return the existing report without adding another progress event.

sp_LabRstList keeps its seven-column result contract for billing/dashboard callers.
The application supplies @ForEntry=1; this compatibility parameter uses the same history-based eligibility.
The default retains coverage for final RSTs with stage 7, 9 or 13 in their history.
No historical business rows were backfilled or assigned invented stages.
The eligibility rule was corrected to include stages 9 and 13 after initial deployment.

Deployment order: RST_Progress_Integration.sql, then Lab_RST_Stage_Workflow.sql,
after all older lab migrations. Do not rerun older lab save/list migrations after
this one. analysis/apply_lab_rst_stage.ps1 requires PowerShell 7, backs up existing
procedure definitions under artifacts/lab-stage, and applies both files atomically.

Validation: analysis/test_lab_rst_stage.ps1 creates and removes an isolated local
SQL Server database. Checks cover idempotent migration, pre-stage-7 rejection,
partial saves, unmapped coverage, repeated partial history, direct full completion,
replay after completion, closed entry, non-final RSTs, incomplete input,
rollback after a simulated progress failure, latest/current consistency, and
downstream billing coverage. No synthetic tests run against the configured database.
Application build passed using a separate output directory because the running
application locked the normal executable output.

History for one RST (largest ProgressId is current):

```sql
DECLARE @RSTNumber nvarchar(50) = N'YOUR-RST';
SELECT p.ProgressId,p.RSTNumber,p.StageId,s.StageName,p.Status,p.IsActive,
       p.StartedAt,p.CompletedAt,p.CreatedBy,p.Remarks
FROM dbo.t_RSTProgressWork p
JOIN dbo.m_RSTStageMaster s ON s.StageId=p.StageId
WHERE p.RSTNumber=@RSTNumber
ORDER BY p.ProgressId DESC;
```
