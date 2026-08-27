# BinTracker Agent Instructions

These are standing repository-wide instructions. Task-specific user instructions control the requested delta, but do not waive repository hard gates unless the user explicitly changes the governing rule. Keep work scoped; preserve unrelated user changes.

## Start with repository truth

Before substantive work, confirm the repository root/branch/status and inspect the affected solution areas. Read the authoritative sections that materially govern the task; targeted reading is preferred to rereading every document blindly.

Practical read order and authority:

1. The current user task defines the requested change. `docs/Roadmap.md` and `docs/RoadmapCoverageMatrix.md` define current scope, sequence, and protected workstreams.
2. `docs/RequirementsAcceptanceRegister.md` is the permanent requirement-ID, scope, and acceptance-status ledger.
3. `docs/BusinessRules.md`, `docs/FunctionalSpecification.md`, and `docs/Architecture.md` govern current business semantics, functional behaviour, boundaries, and invariants. Topic-specific documents refine them (notably `docs/Database.md`, `docs/ImportWizard.md`, `docs/ReimportSafety.md`, `docs/AuditCoverage.md`, and report/master-data documents in `docs/`).
4. `docs/Testing.md` defines validation and manual-test policy; `TEST-CHECKLIST.md` records active/operator acceptance. Source presence or automated success never overrides pending human acceptance.
5. `KNOWN-ISSUES.md` owns current defects/limitations; `TECH-DEBT.md` owns unresolved engineering debt; `docs/SecurityHardeningRegister.md` owns external security findings; `docs/DocumentationAudit.md` records documentation/release-gate reconciliation.
6. `Directory.Build.props` is the version source of truth; `docs/Versioning.md` governs version/package identity. `docs/DevelopmentWorkflow.md`, `Build-BinTracker.bat`, `Audit-BinTracker.ps1`, and `Package-BinTracker.ps1` define the canonical build/audit/package workflow.
7. `docs/CHANGELOG.md` is historical evidence and `docs/RELEASE-NOTES.md` describes the current candidate. Neither overrides current requirements, rules, or acceptance state. `README.md` is a current overview, not a competing specification.

If current authoritative sources genuinely conflict, stop and report the conflict; do not silently select, merge, or invent a new source of truth.

## Hard workflow gates

- **Implement means implement.** When the user unambiguously says implement, do it, start, fix it, go ahead, or equivalent, the proposal/mockup phase is over: change the actual codebase. Do not answer only with mockups, pseudocode, screenshots, or another permission request. Exceptions are an explicitly requested design phase, a genuine unresolved ambiguity, or a repository/data/security gate that requires clarification. Inspection, safety, tests, and governance still apply. The Roadmap's mandatory Dashboard design gate remains an explicit exception.
- **Settle material semantics before implementation.** For changes affecting business or operational/accounting rules, data integrity, persistence/schema, correction/reversal lineage, atomicity, concurrency, authorization/security, audit evidence or architectural invariants, resolve known material semantic and architectural questions before coding. If investigation or implementation exposes a material dependency, contradiction, integrity risk or ambiguity, stop that implementation path and settle the service/data semantics and invariants before presentation code or a workaround can define them accidentally; record the approved design in the appropriate authoritative documents. This gate does not require a separate design phase for routine implementation choices or permit speculative alternatives to delay work: once material semantics are settled and the user says implement, the **Implement means implement** gate applies, and work stops only for a genuine newly discovered material issue.
- **Read governing documents first.** Establish the applicable scope, requirements/acceptance, business rules, architecture, tests, known issues, security/audit obligations, and version/release state before substantive implementation.
- **Preserve accepted behaviour.** Inspect implementation, requirements, and tests before altering established behaviour. Do not remove, simplify, redesign, or opportunistically clean up accepted functionality unless the task requires it. Preserve the distinction between accepted and pending acceptance.
- **Documentation is implementation.** In the same working change, reconcile only the authoritative documents affected by changes to behaviour, requirements, defects, architecture, tests, security/audit status, debt, or release state. Keep current-state documents mutually consistent; put superseded behaviour only in the Changelog. Do not make meaningless documentation edits or claim manual acceptance from static evidence.
- **Permanent requirement IDs stay unique and stable.** Add a permanent ID before implementing/scheduling a new requirement; extend an existing family where appropriate. Never duplicate, recycle, casually renumber, or silently remove an ID. Material change/removal requires the decision record mandated by the register. `Audit-BinTracker.ps1` validation is mandatory.
- **Existing build/audit/package gates remain blocking.** Never weaken, skip, bypass, or claim them passed without their required evidence. Never weaken/delete tests, analyzers, nullability, authorization, or integrity checks to obtain green output. `TreatWarningsAsErrors` remains enabled; fix root causes rather than adding unjustified `!` or suppressions.
- **Security hardening is a protected pre-v1 gate.** Preserve all BT-SH-001..050 IDs, valid dispositions, the Roadmap position immediately after Movement Correction/Reversal and before Branding/Communications, and the v1.0 block while `CONFIRMED-V1`/`REVIEW-V1` findings remain. A `FIXED` disposition requires source/test evidence. Follow `docs/SecurityHardeningRegister.md` and the mechanical audit.
- **Architecture portability/concurrency is permanent.** Preserve the remote-client -> authenticated service/API -> central PostgreSQL target and current provider-neutral Core/Services/contracts/shared EF model. The current product is .NET 8 WinForms/Win32 with a local SQLite adapter. Do not add WinUI/MSIX or speculative migration work unless requested; keep presentation replaceable and business logic testable outside controls.

## Integrity, correction, and security

BinTracker holds operational and append-only audit history. Use authoritative persisted IDs for identity, lineage, batches, and detail navigation; never reconstruct or substitute identity from approximate/display attributes. Fail closed on missing, duplicate, or ambiguous identity. Preserve transaction atomicity, immutable evidence, database-enforced uniqueness, idempotency, optimistic concurrency, authorization, and audit evidence. UI visibility is not a security boundary; enforce roles at service/data boundaries.

For correction/reversal work, follow `docs/BusinessRules.md`, `docs/FunctionalSpecification.md`, `docs/Architecture.md`, and the BT-CORR/BT-AUD requirements. Preserve immutable originals, auditable neutraliser/replacement lineage, effective-history operational views, complete Movement History/Audit evidence, persisted batch identity, whole-batch atomicity, no-op rejection, and duplicate/concurrent-attempt protection. Administrator review is distinct from operational effectiveness. If the requested workflow exposes a model/lineage gap, define correct semantics rather than bypassing the guard.

## UI and maintainability

Windows 11 at 1920x1080 and 150% scaling is the frequent laptop usability floor; substantially larger 24/27-inch production displays must also use space well. Base layout on client/working area and DPI, not physical inches. Keep controls accessible, avoid unnecessary form/horizontal scrolling, and do not globally shrink or change all grids for one screen.

Detail windows should expose structured evidence clearly and wrap genuine prose. Report grids should protect identifiers/operational fields, allocate responsive space to long semantic fields, bound wrapping, and preserve full text through established tooltip/detail behaviour. Preserve sorting, selection, tooltips, export ordering, and other accepted behaviour.

Write clear, focused code with separated UI/business/data responsibilities. Comments explain why: business/security/data invariants, transaction or lineage decisions, persisted-ID assumptions, edge cases, and non-obvious compatibility choices. Do not narrate obvious C#. Use XML documentation where it improves important reusable contracts. Make only safe, in-scope extractions; record larger refactors as debt rather than expanding a corrective task.

## Validation and evidence

Always distinguish:

- implemented in source (`IMPLEMENTED-STATIC` where applicable);
- focused automated tests passed;
- full automated suite passed;
- source/build/package gate passed;
- Windows/operator acceptance passed (`IMPLEMENTED-ACCEPTED` only with explicit evidence).

A user-reported real-application failure is failed acceptance even when automated tests pass. Never mark checklist/manual/DPI/preview/print acceptance complete without actual verification.

During development, run focused builds/tests and meaningful regression tests. Run broader validation for broad, high-risk, architectural, persistence, security, or explicitly full-gate work; do not run the full suite after every small edit. Never substitute shallow source-string checks where behavioural tests are appropriate.

`Build-BinTracker.bat` is the canonical Windows source-audit -> restore -> build -> automated-test gate. It must fail on any failed guarded command, and a valid candidate has zero warnings/errors and no failed tests (or skipped tests where the governing checklist forbids them). The BAT does not prove Windows UI/DPI/interaction or real preview/print/workbook acceptance. Tell the user exactly whether `TEST REQUIRED: None / Targeted / Full` and which checks remain. For focused work, the user may run the canonical BAT externally; run it when the task requires the full gate.

Before **every packaged build**, including documentation-only builds, perform the mandatory full audit in `docs/DevelopmentWorkflow.md`/`docs/Testing.md`: implementation/state review, every Markdown/current-state contradiction review, Roadmap/coverage reconciliation, version reconciliation, requirements/spec/test reconciliation, test classification, and release-document updates recorded in `docs/DocumentationAudit.md`. `Audit-BinTracker.ps1` is the mandatory mechanical source/governance/security gate and protects required docs, version surfaces, requirement IDs/enums, contradictions, roadmap/security ordering, architecture/integrity rules, warnings policy, and selected accepted source paths.

Packaging is separate: `Package-BinTracker.ps1` must first pass the source audit, create `BinTracker-v<Version>.zip` with the sole root `BinTracker-v<Version>`, reopen it, and verify embedded `Version`/`InformationalVersion`. All package/version surfaces listed in `docs/Versioning.md` must match. Do not deliver a mismatched artifact.

Use `git diff --check` before handoff. For a governance-only change, do not spend time on application tests/full BAT unless required; use a useful lightweight audit only when it will not trigger unrelated validation.

## Version, Git, and running application

Follow `docs/Versioning.md`; do not invent a scheme or silently replace an already delivered/tested candidate when a corrective increment is required. Keep governed surfaces consistent, but do not bump for an unaccepted small source edit unless the established release workflow requires it.

Commits are recoverable checkpoints, not acceptance declarations. Prefer coherent checkpoints for substantial work, accurately recording static validation and pending acceptance, but **never commit or push without explicit user instruction**. Do not merge, rebase, reset, force-push, rewrite history, modify `origin/master`, discard unrelated work, or alter user changes without explicit authorization.

Do not terminate the user's running BinTracker process merely because build output is locked. A materially equivalent isolated temporary-output compile may support focused validation and must be reported as such; it never replaces the canonical gate. Ask the user to close the app if the canonical build/package gate requires unlocked output.

## Efficient agent communication and handoff

Use targeted searches/excerpts and inspect source, diffs, tests, history, and docs as needed. Do not dump giant searches/diffs/files, narrate routine mechanics, repeat successful logs/status, or run broad searches unnecessarily. Concisely report material architecture findings, ambiguity, integrity/security concerns, design decisions, gate failures/diagnoses, blockers, and changes of approach.

Final handoff should state: change and root cause/design decisions where relevant; useful file/area summary; focused/full validation and failures resolved; documents/requirements updated; remaining manual acceptance and exact canonical gate requested; and whether work is uncommitted. Do not conflate evidence levels or paste giant diffs.
