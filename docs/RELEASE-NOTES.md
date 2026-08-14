# BinTracker Current Release Notes

## v0.4.0-alpha.19.11.3

### Correction comparison identity fix

Manual smoke testing found that the correction review could show hundreds of false changes after changing only one workbook value.

Cause:
- previous imported movements were grouped using labels normalised to `Blue`;
- proposed reconciliation rows were grouped using the configured display label `Blue Bin`;
- the same physical container therefore appeared as two different correction positions.

Fixed:
- correction comparison now keys configured containers by `ContainerTypeId`;
- customer identity remains normalized by customer code;
- display text is used only for what the operator sees;
- the Greek delta symbol has been removed from correction wording;
- `Replace / Correct` button width is increased so the full action is visible.

### Regression coverage

The changed-workbook integration test now mirrors the real smoke test:

- original Blue OUT = 1;
- corrected Blue OUT = 2;
- no other workbook balance change.

The comparison must return exactly one changed position:
`REPLACECO / Blue Bin: 11 → 12 (+1)`.

Same-day and next-day Manual movements remain preserved on top of the corrected workbook position.

### Documentation audit

Roadmap, Known Issues, Technical Debt, Test Checklist, Re-import Safety, Import Wizard, Business Rules, Functional Specification, Testing and README were reconciled. Manual UI/correction acceptance remains open until this build is smoke-tested.
