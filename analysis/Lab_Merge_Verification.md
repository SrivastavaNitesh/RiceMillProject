# Lab merge verification — 19 September 2026

Resolved the seven unmerged paths. Restored the configurable lab dashboard, single multi-item result form, and role-selected layouts. Removed the legacy object-valued test fields that prevented valid result binding. Kept the incoming Supervisor portal design as one complete layout and repaired the Office form markup. Location selection again uses `o_OfficeLocation`, the same identity space validated by the unloading procedure.

Preserved the incoming optional assignment item feature. `Lab_Item_Workflow.sql` now adds nullable `t_UnloadTransaction.ItemId` when absent and updates `sp_AssignUnloading` to accept it. This planned item does not automatically become an actually unloaded item; lab eligibility still comes from `t_UnloadItem`. No foreign keys are added.

Validation:

- Build passed with zero errors; existing nullable warnings are unrelated.
- 24 HTTP assertions passed against `RiceMillLab_Test_01a0abba`, including login/access control, master/mapping edit/remove, multi-item results, replay protection, validation, reports/CSV and unloading-to-lab availability.
- 11 browser DOM assertions passed against rendered Razor HTML and the actual lab JavaScript, including submitted form fields and preserved selections/results.
- Compatibility migration applied successfully to the isolated test database; assignment with optional ItemId passed and foreign-key count remained zero.
- HTTP regression script now accepts `-UnloadId` and `-UnloadRst` so a fresh assignment can be used without overwriting previously completed synthetic unloads.

Evidence: `lab_merge_http_results.txt` and `lab_merge_ui_results.txt` in this folder.

## Database deployment pending

The merge changed the configured SQL Server to `DESKTOP-EMEJV06`. Connection to that server timed out from this workstation. The configured connection string was preserved. This turn applied no migration to that server or to the main RiceMillDB.

On the intended server, execute the updated `Lab_Item_Workflow.sql` against RiceMillDB before running the merged application, for example:

```powershell
sqlcmd -S 'DESKTOP-EMEJV06' -E -C -d RiceMillDB -b -i Lab_Item_Workflow.sql
```

The prior guide's live-database verification refers to 18 September on `DESKTOP-5AC9ERN\SQLEXPRESS`, not the newly configured server.
