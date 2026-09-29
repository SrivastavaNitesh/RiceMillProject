# Billing module — implementation and verification

Admin entry: /Billing/Index. This is a separate controller, layout and storage from the legacy Settlement section.

## Workflow
- All RSTs: party/mobile, driver, tare/net kg, generated date, unloading supervisor, gate/lab/bill/payment status.
- Details: category row spans, item-wise bag-type columns, location bag/weight totals, gross/tare/net reconciliation, lab results and worker allocations.
- Generate bill is visible only for laboratory-complete RSTs; direct GET and POST are also checked.
- Item rate basis: kg, quintal (100 kg), or bag.
- Default item kg = RST net kg × item bags ÷ total bags. Rounding remainder is applied to the final item so totals reconcile exactly.
- Measured mode requires every item's kg and exact reconciliation with net kg.
- Bag tare kg = sum(bag count × master deduction grams) ÷ 1000.
- Kg/quintal pricing uses net item kg after bag tare. Bag pricing uses recorded bag count.
- Lab deductions reduce payment, not weight. Numeric result below threshold has zero deduction; equal/above uses the configured percent/per-quintal/per-kg/per-bag/fixed payment deduction.
- Existing lab reference text and master deduction are shown in Billing / Lab deduction rules. Ambiguous ranges are not silently interpreted. Configure a numeric threshold and payment deduction basis/value before billing tests with nonzero master deductions.
- Worker quantities come from Meth records; entered per-bag rates produce worker charges. Item payable + worker charges forms subtotal; entered GST percentage is applied to this subtotal.
- Bills save immutable source/rate/rule/calculation snapshots. Later master changes do not alter existing bills.
- Payments are accounting records only. Partial/full payments update balance/status; overpayments and duplicate submissions are prevented.
- Single-location unloading is preallocated. For multiple locations, entered bag allocations must fully reconcile to supervisor bag rows.

## Database
Billing_Module.sql is replayable and installs:
- m_BillingLabRule
- t_BillingBill
- t_BillingPayment
- sp_BillingRstList, sp_BillingSource, sp_BillingSave, sp_BillingRecordPayment

No foreign keys were added. Migration installed in the configured database in a transaction.
Existing gate/lab header/lab result/worker row counts remained 5/1/7/10.
No live bill or payment was created for testing.
Read-only live procedure verification: 5 RST register rows; RST-2026-000004 source returns 6 result sets, 3 supervisor bag rows, 4 worker rows and 7 lab result rows.

## Validation
- 13 pure calculation/validation assertions passed: allocation, bag tare, threshold boundaries, unit pricing, GST, locations, measured mismatch, precision, missing rules and hostile large values.
- 17 isolated HTTP assertions passed: authentication, lab gate, rule save, preview, input validation, bill save/retry, immutable history, partial/full payment, overpayment/retry and Admin-only access.
- 5 browser DOM checks passed: live total parity, category row span, rate change, bag basis, no viewport-level overflow.
- Final invoice, details (case-insensitive RST), rules and register smoke tests passed.
- Desktop and narrow-screen invoice screenshots inspected.
- Final build succeeded with exit code 0. TEMP/TMP were directed to artifacts/billing/sdk-temp for the build because C: was full; no global environment setting was changed.

Evidence and runnable fixtures/tests: artifacts/billing/.
Restart/rebuild the running development application to load the new controller and views.
