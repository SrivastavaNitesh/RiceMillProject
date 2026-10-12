# Lab test deduction/unit verification

24 September 2026: preserved the user's existing deduction/unit additions and completed the read mapping for Deducations, UnitId and UnitName. The list procedure now returns the saved deduction unit ID and LEFT JOINs unit names, retaining tests with no optional deduction unit.

Edit selection uses model binding rather than manually setting option selection. Q is assigned only for a new form; editing a null unit does not silently select Q. Posted unit selection is retained on validation failure. Existing inactive/unavailable units remain visible when editing. Empty table colspan matches all seven columns. Remove errors return a readable validation response.

Build passed with two existing nullable warnings. Twelve HTTP checks passed on the isolated RiceMillLab_Test_01a0abba database (lab_deduction_http_results.txt). Checks cover create/edit, display, selected unit, invalid deduction, null-unit visibility and synthetic soft removal. SQL confirmed the synthetic removed row remains present with IsActive=0. The remove procedure deactivates active mappings and retains stored reports; no remove operation was run against the configured database.

Applied only Lab_Test_Deduction_Display.sql to the configured database. Before/after master value comparison matched. Read verification returned three active master rows with Deducations, UnitId and UnitName columns. The production save procedure already accepts and stores deduction/unit parameters and has QUOTED_IDENTIFIER enabled; it was not replaced. No business rows were removed and no foreign keys were added.

The application was built in artifacts/lab-deduction-check for testing. Restart/rebuild the running application to load the C#/Razor changes. No Git commit or push was made.