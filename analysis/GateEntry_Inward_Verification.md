# GateEntry/Create inward linking — 20 September 2026

- Pending records now come from t_InwardHeader joined to m_InwardTypeMaster: RSTRequired=1, IsRSTGenerated=0, no existing t_GateEntry for that inward, and not exited/settled.
- Dropdown values are no longer lost through casting List<SelectListItem> to SelectList.
- Vehicle and driver selections use the pending inward snapshots. Multiple matching trips require an explicit inward selection; no unrelated previous-trip defaults are chosen. Driver name/mobile are considered together.
- Company Name replaces Target Office Location; Target Unloading Locations is removed from this form and saving path.
- sp_CreateRstFromInward validates pending status, weight and company; uses transaction/application locking and a SQL sequence; saves the inward link and sets IsRSTGenerated. A repeated submission is rejected. RST format is RST-<year>-<sequence>; sequence gaps are allowed.
- The inward owns vehicle/driver/party details. Posted vehicle/driver IDs cannot override it. Missing vehicle/driver master rows are created from the inward snapshots transactionally; inactive or ambiguous matching masters are rejected.
- Gateman inward currently has no material/item field. This change auto-fills the related inward, vehicle, driver/mobile and party details; it does not invent material associations.
- No foreign keys introduced. No real RST was generated during verification.

Validation: build succeeded using artifacts/inward-check because an existing running app locked the normal executable. Seven HTTP assertions, eight browser DOM assertions and SQL persisted-link/duplicate checks passed against RiceMillLab_Test_01a0abba. Results are in gate_entry_http_results.txt and gate_entry_ui_results.txt. Existing unrelated nullable build warnings remain.

GateEntry_Inward_Workflow.sql was applied to the configured DESKTOP-5AC9ERN\SQLEXPRESS / RiceMillDB. Read verification returned INW-2026-000001 and zero foreign keys. Restart/rebuild the running application to load the changed controller and Razor view. Existing user edits in other pages and appsettings.json were preserved.