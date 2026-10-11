# Billing rules update

- Billing remains restricted to current stage 14/15.
- Unmapped items have no required lab tests and do not block billing. Mapped tests
  with missing results still prevent saving. No lab results are fabricated.
- Multiple items default to net kg multiplied by item bags / total bags. This is
  an estimate assuming equal average bag weight; the final row absorbs rounding.
- Single-item RSTs use FinalNet, assigning the entire net kg to that item.
- Weight selection and editable weight inputs are removed. The backend uses
  optional supervisor-detail ActualWeightKg first; remaining net kg is distributed
  among unmeasured bags. Posted mode/weight values are ignored. Missing actual
  measurements remain NULL; no measurements are fabricated.
- Derived multi-item weights and their billable kg show an asterisk with a Hindi
  explanation. Actual and single-item final weights do not show the marker.
- Bag tare is deducted afterward for kg/quintal pricing. Saved actual weights
  must reconcile with net weight; contradictions block saving.
- Supervisor detail LocationId supplies actual location bag quantities; billing
  does not ask users to allocate bags again or block on absent Meth reconciliation.
- Palledari is one manually entered RST amount, default 0. It replaces worker-wise
  charges for new bills and is stored in the snapshot and existing WorkerAmount
  column. Original invoices keep their saved amounts and worker-charge breakdown.
- Hindi hints explain weight and deduction formulas. Essential save checks appear
  in a compact disclosure instead of a large warning banner. All billing tables
  use table-bordered.
- Existing deduction rules retain their meaning: result >= threshold triggers
  the full configured deduction, not just the amount over the threshold.

Run calculation checks:

```powershell
dotnet run --project analysis/billing-calculation-tests/BillingCalculationTests.csproj
```

26 assertions cover the user's 17000 kg example (4250, 7083.333, 5666.667), single
item weight, bag tare, no-test items, Meth mismatches, actual locations, zero and
positive palledari, persistence, GST, invalid amounts and lab threshold boundaries.

Deployment: run analysis/apply_billing_stage.ps1 in PowerShell 7. It backs up and
updates only sp_BillingRstList, sp_BillingSource and sp_BillingSave atomically.
No bill or payment is created in the configured database for testing.
