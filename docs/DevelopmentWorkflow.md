# Development Workflow

## Implementation passes

For each meaningful BinTracker change:

1. Implement code immediately when the user asks to implement; do not substitute a mockup unless explicitly requested.
2. Add/retain automated regression coverage where practical.
3. Perform the full documentation audit defined in `docs/Testing.md`.
4. Produce the candidate build/ZIP.
5. Run `Build-BinTracker.bat` on Windows as the canonical restore/build/test gate.
6. Perform the required manual smoke level:
   - UI → Full;
   - business logic → Targeted;
   - UI + logic → Full;
   - reports → real preview/print;
   - importer → real workbook when applicable.
7. Only after acceptance should the candidate be committed/pushed as the accepted checkpoint.

## Build gate

`Build-BinTracker.bat` is the canonical local gate. It must return failure when restore, compilation or automated tests fail; a misleading success banner is itself a release-blocking defect.

## Documentation discipline

Current-state documents must describe the current product, not accumulate contradictory historical alpha notes. Release history belongs in `docs/CHANGELOG.md`.
