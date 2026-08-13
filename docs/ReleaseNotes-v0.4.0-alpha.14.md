
# BinTracker v0.4.0-alpha.14 — Legacy Container Inference

- Added industry-default container inference for the legacy workbook profile.
- No explicit legacy container token now resolves to Blue Bin.
- `(Y)` continues to resolve to Yellow Bin.
- `(Bulk)` continues to resolve to Bulk Bin.
- Configured Container Type names and short codes remain valid explicit mappings.
- Unknown explicit tokens such as `(Tub)` are never guessed and remain blocked for manual mapping.
- Balance Reconciliation now shows a Container rule/reason column explaining defaulted, resolved, or unknown mappings.
- Added regression tests for implicit Blue Bin and unknown explicit tokens.
- No database Import execution enabled yet.
