# RST item-wise laboratory workflow

Implemented and verified on 18 September 2026. The migration has been applied to the configured `RiceMillDB`. No foreign keys were created; the verified live count remains zero. Existing business records were not deleted. Synthetic data was used only in the separate `RiceMillLab_Test_01a0abba` database.

## How to use

1. Open **Lab → Test Master** (`/Lab/Tests`). Add each test's name, numeric/text result format, optional unit and instructions. The real master starts empty because the test list is still to be supplied.
2. Open **Item Test Mapping** (`/Lab/Mappings`). Select a category, an item within that category and a test. Add one row per required test. Both forms support editing and removing entries. Removing a test also removes its active mappings; previously saved reports remain available.
3. On **Meth → Execute Unload**, select all items actually unloaded. Item IDs and their category IDs are saved with that unloading. Assignment can now exist before an actual location is selected; final submission requires the location and saves unloading/items/workers/location together.
4. A lab technician signing in is routed to **RST Quality Checks** (`/Lab`). Admins can also access these pages. Only RSTs with item-wise unloading in Unloaded/Verified state appear.
5. Choose **Enter results** for an RST. The category dropdown supports multiple checkbox selections and contains only that RST's unloaded categories. Select multiple items below it. Their currently mapped active tests appear as input fields. Existing typed values survive selection changes.
6. Fill every test for the selected items and save. The logged-in operator and timestamp are recorded automatically. Repeated submission of the same form does not create a duplicate report.
7. Open **Lab Reports** (`/Lab/Reports`) to filter by exact RST number and date range. Each report has a detailed grouped view, Print/PDF through the browser print dialog, and CSV download.

Historical reports store category/item/test names, units and result formats at save time. Editing/removing the masters does not rewrite those reports. New entries create additional reports; test coverage on the RST screen counts whether each currently required item/test has a saved result.

## Database objects

| Table | Purpose |
|---|---|
| `m_LabTestMaster` | Tests, result formats, units and instructions |
| `m_ItemLabTest` | CategoryId, ItemId and TestId mapping; active/inactive history |
| `t_UnloadItem` | The actual items/categories recorded against an unloading |
| `t_LabReport` | RST, unique submission token, operator, time and remarks |
| `t_LabReportResult` | Per-item/test results and historical display snapshots |

Validation uses procedure/application checks and local primary/unique indexes. No relationship constraints or cascade behavior were introduced.

The incremental script is [Lab_Item_Workflow.sql](<F:/Net Core Practice Project/RiceMillProject/Lab_Item_Workflow.sql>). It can be reapplied without erasing report or master data. It also makes the old mandatory unloading LocationId nullable during assignment. The final unloading procedure still requires a valid active location.

## Verified behavior

- Build passed, including Razor views. Four pre-existing warnings remain in `PersonDAL.cs` and `DhermKata.cs`.
- SQL integration assertions passed: multi-category/item saving, duplicate/replayed operations, invalid cross-category mapping, another RST's item, missing/duplicate results, numeric validation, non-lab user rejection, historical report preservation, and mapping edit/remove.
- 24 HTTP assertions passed: lab login and access control, master/mapping forms, report save/retry/read/export/filtering, invalid numeric result preservation, antiforgery protection, and the full unloading form → lab item availability path.
- 11 headless Chrome DOM assertions passed against rendered Razor HTML and actual local JavaScript: category filtering, multi-selection, mapped tests, form field association, deselection, preserved values and save availability. A screenshot was also visually inspected. The in-app browser tool was unavailable, so this check used a separate headless profile and synthetic data.
- Migration applied to the main database, followed by successful read-procedure checks. The main test master/report tables are empty and ready for real setup.

Evidence: [SQL tests](<F:/Net Core Practice Project/RiceMillProject/analysis/lab_integration_results.txt>), [HTTP tests](<F:/Net Core Practice Project/RiceMillProject/analysis/lab_http_results.txt>), [UI tests](<F:/Net Core Practice Project/RiceMillProject/analysis/lab_ui_results.txt>), [live verification](<F:/Net Core Practice Project/RiceMillProject/analysis/lab_live_verification.txt>).

## Boundaries

- Existing unloading records without item details are not guessed or populated automatically. They need accurate item-wise data before lab testing. There were zero unloading rows in the main database at migration time.
- Test definitions and deduction/acceptance rules have not been invented. These new reports save observations; they do not automatically alter legacy settlement deductions or mark a financial settlement complete. The old fixed four-test calculation remains separate from the new configurable workflow.
- Earlier gate-entry/registration issues documented in the database analysis are outside this lab change. The end-to-end test here begins with an existing RST/unloading assignment.
- Existing user changes in the Office creation and Supervisor layout files were preserved. The Office DAL location lookup was corrected to the existing `o_OfficeLocation` table because unloading uses it.
- The isolated test database is retained for reproducibility; the temporary test app is stopped after verification. Its sample tests/accounts are not added to the main database.
