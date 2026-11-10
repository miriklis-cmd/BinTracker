# Versioning

BinTracker currently uses semantic pre-release version `0.5.0-alpha.21`.

## Single source of truth

The release version is defined in the repository root:

`Directory.Build.props`

```xml
<Version>0.5.0-alpha.21</Version>
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

`0.5.0-alpha.11` is the next feature/development increment for the Security Hardening workstream. Its first bounded pass fixes BT-SH-001 across every current report CSV export without overwriting accepted alpha.10 architecture evidence. Later BT-SH findings remain separately governed in register order.

`0.5.0-alpha.12` is the next feature/development increment for BT-SH-002. It hardens the pending database-operation marker's Data-owned structured-input/path boundary without overwriting accepted alpha.11 CSV evidence. BT-SH-003 and later findings remain separately governed in register order.

`0.5.0-alpha.13` is the next feature/development increment for BT-SH-003. It makes desktop logout and new-login attempts atomically clear authenticated `UserSession` state without overwriting accepted alpha.12 marker-hardening evidence. BT-SH-004 and later findings remain separately governed in register order.

`0.5.0-alpha.14` is the next feature/development increment for the reconciled Security Hardening Batch A. It implements canonical bounded authentication identity, current-account revalidation and uniform Services authorization without overwriting alpha.13 session-lifecycle evidence. Batch B and later hardening remain separately governed by the Security register.

`0.5.0-alpha.15` is the accepted Security Hardening Batch B increment. It closes BT-SH-006/030 with Data-owned transactional mutation/audit consistency without overwriting accepted alpha.14 identity/authorization evidence. BT-ARCH-018-P4's exact candidate passed the operator BAT under SDK 10.0.402 at 265 permanent IDs across 27 Markdown files and 901/901 tests, then was committed/pushed as `9f2e6b6`. The later governance-only P6/FIX1 work did not require a version bump.

`0.5.0-alpha.16` is the accepted Security Hardening Batch C increment. It closes BT-SH-015/016/039/040/044 through provider-neutral enum/input guards, checked quantity arithmetic, authoritative master-data maxima and a Unicode-preserving operational-identifier policy. Independent source review passed; the exact-candidate canonical BAT passed 931/931 tests with zero failures/skips and a successful zero-warning/error build; the implementation is committed/pushed as `41735cfdc4179bcc8a8bf8b5579b9833ea9a3150`.

`0.5.0-alpha.17` is the accepted Security Hardening Batch D increment. It closes BT-SH-038/043 through governed schema18 SQLite string/singleton constraints, exact UTF-16 measurement, malformed-retained-data preflight, verified backup and atomic rollback/retry semantics. Independent FIX2 review/input/handoff validation passed; Jack/operator's exact accepted candidate passed the canonical BAT 959/959 with zero failed/skipped and a successful build, and the result was committed/pushed as `462266c1adbfc741dcc957fe7d267406cf915fd3`.

`0.5.0-alpha.18` is the accepted Security Hardening Batch E milestone. It closes BT-SH-012/013/014/036/037 and implements BT-BATCH-012 through stable-user-ID owner-specific drafts, Services owner enforcement, bounded exact recovery parsing, unique durable temporary publication and collision-resistant no-overwrite quarantine. FIX3 strengthens the same candidate with schema-4 monotonic Save/Clear fencing, payload-free clear tombstones and one durable `ClientOperationId` per logical draft so post-commit cleanup failure/restart replays exactly. Independent review passed; the exact-candidate canonical BAT passed 998/998 with zero failed/skipped and a zero-warning/error build; commit `9bbb01a23a04e56a6f0513c34af5de95798a5ecc` is pushed.

`0.5.0-alpha.19` is the accepted Security Hardening Batch F release. It closes BT-SH-027/033/034/035 at static/focused-automated evidence level through Data-owned SQLite connection-string construction, Windows physical-file identity, an unconditional no-overwrite backup policy and leased flushed atomic settings/pending-marker publication. Independent review passed, the exact-candidate canonical BAT passed audit/restore/build and 1011/1011 automated tests with 0 failed/skipped under SDK 10.0.403, and commit `1b95e5b` is pushed. The distinct retained-data, production Backup/Restore, package/deployment and v1 acceptance scopes remain pending.

`0.5.0-alpha.20` is the Security Hardening Batch G candidate. It closes BT-SH-005 at static/focused-automated evidence level through bounded workbook file bytes, ZIP directory/entries, declared and actual expansion, XML parts/elements and worksheet row/cell/used-range work before ClosedXML. It preserves accepted alpha.19 evidence. Independent review, the exact-candidate canonical BAT and real production-workbook acceptance remain pending; Batch H is not included.

`0.5.0-alpha.21` is the accepted Security Hardening Batch H application milestone. It closes BT-SH-047/048/049/050 through exact SDK pinning, committed dependency locks, fail-closed online advisory scanning, SDK security-category analyzers and verified external package SHA-256 manifests. Checksums prove artifact integrity only; publisher signing requires a real certificate/external release decision. Independent review passed and accepted application commit `94c43d7138ba0e52566575ffb303518fd5530c86` passed the exact-candidate canonical BAT with 1024/1024 tests, zero failed/skipped and zero warnings/errors. The later documentation-only bookkeeping commit exposed audit/governance defects; FIX8 corrects those tools without changing application or package identity. Consistent with the accepted alpha.15 governance-only precedent, this correction retains `0.5.0-alpha.21`, but because audit-script changes are candidate-defining it requires independent source review, a fresh canonical BAT in the no-write verification phase and separate BAT-evidence approval before another Git commit.

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

`Package-BinTracker.ps1` must create `BinTracker-v<Version>.zip`, stage the sole root as `BinTracker-v<Version>`, reopen the archive, verify embedded Version/InformationalVersion, emit `BinTracker-v<Version>.zip.sha256`, reread the manifest and rehash the ZIP before a package is treated as releasable. The checksum is integrity evidence, not publisher authentication or a cryptographic signature. A real signing certificate/external signing service remains an explicit release decision. `docs/RequirementsAcceptanceRegister.md` is also part of the source audit gate.
