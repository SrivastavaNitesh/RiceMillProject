# Dashboard/RST display verification

The saved RST-2026-1 was missing because sp_GetAllGateEntries did not return fields required by GateEntryDAL (including InwardNo and TargetOfficeId). Updated the read procedure to provide the complete column contract and linked inward/driver details. The register now displays query failures instead of silently appearing empty.

Updated dashboard counts to include weight-done RSTs, count an inward/RST trip once, and use current lab test coverage. Corrected Weightman/Gateman routing, the RST action-center link, and the row's unloading link to Unload/Assign.

Dashboard_Rst_Workflow.sql was applied to the synthetic database first, then configured RiceMillDB. Live verification: TotalEntered=1, PendingUnload=1, PendingLab=0, PendingSettlement=0; RST-2026-1 links to INW-2026-000001; foreign-key count remains zero. No business records were changed by this migration.

Build passed with two existing nullable warnings. Nine HTTP assertions passed (dashboard_rst_http_results.txt), including saved-row rendering and action links. Build used artifacts/dashboard-check to avoid the user's running executable lock. Restart/rebuild the running app for controller/view changes; stored-procedure fixes apply immediately.