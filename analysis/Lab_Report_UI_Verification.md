# Lab report and interface verification — 25 September 2026

## Fix
The application accepts Admin user type 4 and Lab Technician user type 5.
sp_LabReportSave previously checked post IDs 1 and 5, rejecting Admin4.
Reproduced the rejection in a rolled-back isolated transaction.
Lab_Report_Save_Access.sql aligns saving with user type / role checks and retains
existing result validation, transaction, snapshots, and submission deduplication.
Applied to the configured database. Report/result counts stayed 0/0.
No foreign keys or business-data deletions were introduced.

## Interface
All Lab routes use the shared Laboratory sidebar, including Admin visits.
Shared styling covers RST list, test master, item mappings, entry, report filters,
and report details. Sidebar collapses on smaller screens. Printed reports hide
navigation and action controls.

## Verification
- Build succeeded: 0 errors, 2 pre-existing nullable warnings.
- 21 isolated HTTP assertions passed, covering Lab and Admin4 save, report redirect,
  report list, CSV, repeated submission, validation retention, navigation, and
  worker access rejection.
- 7 browser DOM assertions passed for multi-item selection, result retention,
  submitted IDs, save state, sidebar open/close, and viewport fit.
- Inspected desktop entry, test master, report, and narrow-screen entry screenshots.
- Test database: RiceMillLab_Test_01a0abba. No real reports were created for testing.
- Evidence and runnable test script: artifacts/lab-refresh/.
## Follow-up: mapping alignment and completion lock
- Mapping table now groups item/category, aligns assigned tests with removal actions, and keeps edit actions in a separate column.
- Save success is shown only after the returned report ID can be read from the database. False IDs 0 and 2147483647 were rejected in isolated tests with entered values preserved.
- RST-2026-000004 was subsequently saved as LAB-1 (2 items, 7 results); verified through both report procedures. A third unloaded item (Rice Bran) currently has no mapped tests.
- Fully completed RSTs disable entry. GET redirects to reports. SQL serializes saves per RST, rejects new submissions after full coverage, and preserves retries of the same submission.
- Completion uses the existing sp_LabRstList rules, including supervisor-selected items and unmapped item counts. No supervisor procedures were replaced.
- Lab_Report_Completion_Lock.sql applied to configured DB; existing report/result counts remained 1/7. Apply this after Lab_Report_Save_Access.sql on other databases.
- Seven completion HTTP assertions passed (partial/open, final save, retry, disabled button, direct URL, stale POST, unmapped item).
- Entry UI now has aligned test/result/reference columns; desktop and narrow-screen screenshots captured under artifacts/lab-completion.
- Compilation completed with 0 errors and 2 existing warnings. SDK process teardown reported a logging exception while C: had no free space.
