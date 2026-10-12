# Admin purchase reports

Five separate routes in the Purchase Reports sidebar dropdown and page selector:
Monthly, Daily, DateRange, Party, Locations; LocationDetails opens a location.
All routes, including Excel downloads, use the existing admin-only BillingAccess policy.

## Data rules

- Date basis: `t_GateEntry.GateEntryTime`, the RST weighing entry date, not gate inward date or bill date.
- India local current month/year/day defaults. Date range end is inclusive using a next-day exclusive SQL bound.
- Company comes from `t_GateEntry.TargetOfficeId`. Only inward RST records are included.
- Default company is the active `Main Mill` master record, resolved by name rather than a hardcoded ID. Reports show one selected company; changing the company filter is supported. The company is identified in the filter and Excel metadata, not a separator row inside the report table.
- Completed supervisor detail rows provide item, category, bag type, count and location.
- Missing detail locations are resolved only when exactly one unload location is recorded. Ambiguous locations remain Unassigned.
- Actual detail weights are preserved. Remaining RST net weight is allocated by unknown detail bag counts, rounded to 3 decimals with the final detail receiving the remainder.
- A single unknown lot receives full net weight without an estimated marker. Split weights receive `*`.
- Missing final net weights remain Pending; they are not fabricated as zero. Invalid actual totals generate a reconciliation notice.
- Locations show cumulative recorded purchase unloading, not stock after outward movements. All location master records are included; company selection respects mappings and historical recorded unloads.
- Lab results are latest per RST/category/item/test. Unsubmitted mapped tests are Pending. Unmapped items are Not applicable. Submitted results show threshold-based configured deduction rates and units; where no billing rule exists, a positive master deduction is explicitly labelled as such. No fictitious monetary total is computed without a purchase price.
- Excel uses the same table builder as the screen, real `.xlsx`, merged headers/row spans, numeric weights (including starred weights), and literal text for names and phone numbers.

## Verification

`dotnet run --project analysis/purchase-report-tests/PurchaseReportTests.csproj -- --database`

Includes deterministic weight/date/lab tests, Excel OpenXML schema validation, formula-string safety, phone leading zeros, and read-only live company/date filter tests.
`analysis/test_purchase_query.ps1` independently validates SQL result shapes and unique unload detail IDs.

No business records, stored procedures, stages or master data are modified by these reports or tests.
