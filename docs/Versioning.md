# Versioning

BinTracker currently uses semantic pre-release version `0.5.0-alpha.10`.

## Single source of truth

The release version is defined in the repository root:

`Directory.Build.props`

```xml
<Version>0.5.0-alpha.10</Version>
```

The same value is used by:

- .NET assembly package/informational version metadata;
- the application's status bar through `AppVersion`;
- `Build-BinTracker.bat`;
- future packaging/installer scripts.

Do not add a second hard-coded application version to WinForms code.

## Releasing a new build

For a release:

1. Update `Version` and `InformationalVersion` in `Directory.Build.props`.
2. Update numeric `AssemblyVersion` / `FileVersion` only when appropriate.
3. Update CHANGELOG, release notes and TEST-CHECKLIST.
4. Run `Build-BinTracker.bat`.
5. Confirm the BAT header and BinTracker status bar display the same version.

## Acceptance and Git workflow

Typical alpha workflow:

1. Create a versioned candidate.
2. Run `Build-BinTracker.bat`.
3. Complete the required manual smoke level from `docs/Testing.md`.
4. Fix defects before acceptance; do not mark checklist items complete prematurely.
5. Commit/push the accepted checkpoint to GitHub.
6. Use tags/releases for meaningful milestones when appropriate.

The build script must truthfully propagate restore/build/test failures.


## Pre-release milestone policy

Version milestones follow **real scope boundaries**, not a fixed count of phases.

Examples:

- `0.4.0-alpha.*` — current operational completion / Reports phase.
- A substantial new workstream such as Dashboard may start a new minor milestone, e.g. `0.5.0-alpha.1.1`.
- Communications, correction/reversal, production operations, PostgreSQL/multi-user or other major workstreams may each receive their own `0.x.0` milestone when their scope justifies it.
- It is valid to use `0.10.0`, `0.11.0`, etc. if additional pre-1.0 milestones are needed.

Do not force all remaining work into a pre-decided `0.5 → 0.6 → 1.0` sequence.

## Alpha increment rule

For **new milestones from the next clean milestone onward**, use:

- Feature/development increment: `0.5.0-alpha.1.1`
- Next feature/development increment: `0.5.0-alpha.2`
- Bug/polish fix specifically to alpha 2: `0.5.0-alpha.2.1`
- Another fix to alpha 2: `0.5.0-alpha.2.2`
- Next feature after those fixes: `0.5.0-alpha.3`

Hard rule: **maximum two numeric components after `alpha`** for the new scheme.

`0.5.0-alpha.9.1` is the corrective successor to the already built/tested alpha.9 candidate after Windows smoke exposed a startup defect before acceptance; it does not overwrite alpha.9 history.

`0.5.0-alpha.9.2` is the next correction to that same alpha.9 milestone after the built/tested alpha.9.1 candidate reached P4 Windows smoke and exposed an operator-comprehension defect in the disposition dialog. It preserves both alpha.9 and alpha.9.1 as tested historical candidates.

`0.5.0-alpha.9.3` is the next correction to the same alpha.9 milestone after the built/tested alpha.9.2 candidate entered end-to-end Windows smoke and exposed a lineage-output physical-batch routing defect plus bounded Movement History/dialog presentation defects. It preserves alpha.9 through alpha.9.2 as tested historical candidates.

`0.5.0-alpha.9.4` is the corrective successor after alpha.9.3 passed the canonical 800/800 gate but renewed Windows smoke exposed a stale operator-baseline defect when an older original physical batch was used as the whole-root correction anchor. Alpha.9.3 remains historical build/smoke evidence and is not overwritten.

`0.5.0-alpha.9.5` is the corrective successor after alpha.9.4 passed its canonical 800/800 gate and controlled retained schema16-to17 rehearsal, but protected Batch #30 Windows acceptance exposed loss of the selected physical-batch anchor after a separately committed Restore. Alpha.9.4 remains historical build/rehearsal evidence and is not overwritten.

`0.5.0-alpha.9.6` is the corrective successor after alpha.9.5 passed the core protected Batch #30 FIX6 continuation but the same Windows rehearsal exposed Movement History date-picker coupling, stale/reordered report refreshes and a missing post-Restore refresh when the later correction dialog was cancelled. Alpha.9.5 remains historical Windows evidence and is not overwritten.

`0.5.0-alpha.9.7` is the corrective successor after alpha.9.6 entered item-6 Windows acceptance and successful Daily, Weekly Detail/Overview, Monthly and Outstanding CSV exports exposed incomplete audit descriptions that omitted filename and active filter/view context. Alpha.9.6 remains historical build and Windows evidence and is not overwritten.

`0.5.0-alpha.9.8` is the corrective successor after alpha.9.7 underwent real Item-8 Windows acceptance at 1920×1080/150% and exposed Movement History width allocation, physical Batch Detail date clipping and an orphaned native grid tooltip across report navigation. Alpha.9.7 remains historical canonical-build and Windows evidence and is not overwritten.

`0.5.0-alpha.9.9` is the corrective successor after alpha.9.8 passed its 824/824 canonical BAT but then failed real Item-8 Movement History layout acceptance because synthetic sortable-header width reservation inflated structural columns, reduced Notes/Status and still required horizontal scrolling. Alpha.9.8 remains historical build and failed Windows-acceptance evidence and is not overwritten.

`0.5.0-alpha.9.10` is the corrective successor after alpha.9.9 passed its 826/826 canonical BAT but renewed real Item-8 Windows acceptance found the Resolve Reversed Batch Lines Date value truncated at 1920×1080/150%. Alpha.9.9 remains historical build and failed Windows-acceptance evidence and is not overwritten.

`0.5.0-alpha.10` is the next feature/development increment after the accepted alpha.9 correction series. It carries the BT-ARCH-018 protected layer-delineation remediation without overwriting alpha.9.10's canonical-build and Windows-acceptance evidence. Independent review passed, and the exact-candidate canonical BAT passed audit/build with zero warnings/errors and 841/841 automated tests with none failed/skipped under SDK 10.0.401. The accepted implementation is commit `cd53f9ca19de1a10ea9ec375b63e294fdddfad23`; later documentation-only reconciliation is not covered by that BAT.

Do not create forms such as:

- `alpha.3.1.1`
- `alpha.4.2.1`
- deeper nested alpha suffixes.

The existing `0.4.0-alpha.20.0.x` history is retained as historical truth and is **not renumbered retrospectively**. The clean rule begins at the next genuine milestone.

## Bug-fix builds in the current legacy 0.4 series

Until the next clean milestone starts, a direct bug-fix to an already-issued legacy candidate may use one additional `.1`, `.2`, etc. suffix solely to avoid rewriting history. This is transitional only and must not continue into the new milestone scheme.


## Package identity gate

For every packaged candidate, all of the following must match exactly:

- ZIP filename version;
- the ZIP's single top-level folder version;
- `Directory.Build.props` `Version`;
- `Directory.Build.props` `InformationalVersion`;
- build-script displayed version;
- README / Known Issues / current Release Notes / Roadmap current baseline.

Packaging mismatch is a release-blocking audit failure.


## Mechanical package gate

`Package-BinTracker.ps1` must create `BinTracker-v<Version>.zip`, stage the sole root as `BinTracker-v<Version>`, reopen the archive, and verify embedded Version/InformationalVersion before a package is treated as releasable. `docs/RequirementsAcceptanceRegister.md` is also part of the source audit gate.
