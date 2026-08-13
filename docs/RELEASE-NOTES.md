# BinTracker Current Release Notes

## v0.4.0-alpha.19.11.1

### Same-cutover correction baseline fix

The alpha.19.11 replacement regression test exposed a real bug: the previous ImportRun was excluded, but legitimate post-cutover Manual activity was still included in the corrected opening-balance baseline.

The corrected rule is now:

- replacement reconciliation uses legitimate history strictly **before** the cutover date;
- previous ImportRun movements are excluded/replaced;
- Manual/Batch activity on the cutover date and later remains untouched and sits on top of the corrected imported position.

Regression coverage now includes a Manual OUT on the cutover date and another on the following day. Both must survive the correction and remain unlinked to the ImportRun.

### Documentation audit

Roadmap, Technical Debt, Test Checklist, Re-import Safety, Import Wizard, Business Rules, Functional Specification, Testing and README were reconciled with the corrected semantics.
