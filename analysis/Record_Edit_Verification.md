# Record editing and history

Implemented for Gateman inward, Weightman RST, Supervisor assignment/field details and Meth/Supervisor labour work.

- Gate Inward Report: /Gateman/GateInwardReport. A Gateman sees only that login's own entries.
- Corrections/history: /RecordEdit/Index. Existing Weightman report and Supervisor/Meth saved-entry registers also provide actions.
- Admin parameters: /RecordEdit/Policies. Policy IDs are assigned by module and active person designation/PostId. Defaults: Gateman 720 minutes; Supervisor/Meth 1440 minutes; Admin 10080 minutes; Weightman until final outward.
- Deadlines use the original entry timestamp, not the last edit. m_RecordEditModuleClock records the source timezone (inward uses IST; other existing procedures use the database server timezone). Named timezones account for daylight-saving changes.
- Finalization is permanent for operators. Admin can correct finalized entries only inside the configured Admin window; editing does not reopen the record.
- Every new insert/update/delete in the covered transaction tables is audited by triggers. Corrections and finalization include actor and policy context. Existing rows start with a BASELINE event, not a fabricated insert event.
- Optimistic version checking and transaction locks prevent stale saves and save/finalize races. Failed saves roll back both data and audit history.
- Validation covers ownership/company, active master selections, numeric ranges, item/category consistency, assigned locations, worker/Meth relationships and material/worker bag totals. Identity/weight edits that would break linked RST chains are rejected with an explanation.
- Party mobile values and columns are removed for Weightman, Supervisor and Meth; driver contacts are retained.

Database migration: Record_Edit_Workflow.sql (SQL Server 2022+, named server timezone support). Reapplying preserves customized policy values.

Run isolated regression tests (PowerShell; local SQL Express required):

    dotnet run --project analysis/record-edit-tests/WorkflowChecks.csproj -p:UseAppHost=false -p:NuGetAudit=false -- "F:\Net Core Practice Project\RiceMillProject"

Tests create a randomly named local SQL database, seed synthetic data, run service/HTTP checks and remove the test database. They never copy production records.

Validated: 47 assertions, application build and release publish. Production business entries were not altered by testing; configured database was migrated with policy/state/history structures and baseline history.
