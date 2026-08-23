param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Fail([string]$message) {
    Write-Host "AUDIT FAILED: $message" -ForegroundColor Red
    exit 1
}

[xml]$props = Get-Content -LiteralPath 'Directory.Build.props'
$version = [string]$props.Project.PropertyGroup.Version
$infoVersion = [string]$props.Project.PropertyGroup.InformationalVersion
if ([string]::IsNullOrWhiteSpace($version)) { Fail 'Directory.Build.props Version is missing.' }
if ($version -ne $infoVersion) { Fail "Version ($version) and InformationalVersion ($infoVersion) differ." }
$expected = "v$version"

$currentChecks = @(
    @{ Path='README.md'; Pattern="^# BinTracker $([regex]::Escape($expected))$"; Description='README current version' },
    @{ Path='KNOWN-ISSUES.md'; Pattern="^Current release: \*\*$([regex]::Escape($expected))\*\*$"; Description='Known Issues current release' },
    @{ Path='TEST-CHECKLIST.md'; Pattern="^Current baseline: \*\*$([regex]::Escape($expected))\*\*$"; Description='Test Checklist baseline' },
    @{ Path='docs/RELEASE-NOTES.md'; Pattern="^## $([regex]::Escape($expected))$"; Description='Release Notes candidate version' },
    @{ Path='docs/Roadmap.md'; Pattern="^Current planning baseline: \*\*$([regex]::Escape($expected))\*\*$"; Description='Roadmap planning baseline' },
    @{ Path='docs/RequirementsAcceptanceRegister.md'; Pattern="^Current baseline: \*\*$([regex]::Escape($expected))\*\*$"; Description='Requirements register baseline' }
    @{ Path='docs/Architecture.md'; Pattern="^Current baseline: \*\*$([regex]::Escape($expected))\*\*$"; Description='Architecture baseline' }
)
foreach ($check in $currentChecks) {
    if (-not (Test-Path -LiteralPath $check.Path)) { Fail "Missing $($check.Path)." }
    $content = Get-Content -Raw -LiteralPath $check.Path
    if ($content -notmatch "(?m)$($check.Pattern)") { Fail "$($check.Description) does not match $expected." }
}

if (Test-Path -LiteralPath 'global.json') { Fail 'Unexpected global.json is present. BinTracker currently uses the installed compatible SDK.' }

$requiredDocuments = @(
 'README.md','KNOWN-ISSUES.md','TECH-DEBT.md','TEST-CHECKLIST.md',
 'docs/Architecture.md','docs/AuditCoverage.md','docs/BusinessRules.md','docs/CHANGELOG.md','docs/Database.md',
 'docs/DevelopmentWorkflow.md','docs/DocumentationAudit.md','docs/FunctionalSpecification.md',
 'docs/ImportWizard.md','docs/LegacyContainerRules.md','docs/MasterData.md','docs/RELEASE-NOTES.md',
 'docs/ReimportSafety.md','docs/Roadmap.md','docs/RoadmapCoverageMatrix.md','docs/Testing.md',
 'docs/Versioning.md','docs/RequirementsAcceptanceRegister.md','docs/ReconciliationReport.md','docs/SecurityHardeningRegister.md'
)
foreach ($doc in $requiredDocuments) { if (-not (Test-Path -LiteralPath $doc)) { Fail "Missing required audited document: $doc" } }

# Permanent requirements register: unique IDs + approved enum values.
$register = Get-Content -LiteralPath 'docs/RequirementsAcceptanceRegister.md'
$reqRows = @()
foreach ($line in $register) {
    if ($line -match '^\|\s*(BT-[A-Z0-9-]+)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|') {
        $reqRows += [pscustomobject]@{ Id=$matches[1]; Scope=$matches[2].Trim(); Status=$matches[3].Trim() }
    }
}
if ($reqRows.Count -lt 100) { Fail "Requirements register unexpectedly small: $($reqRows.Count) IDs found." }
$duplicates = $reqRows | Group-Object Id | Where-Object Count -gt 1
if ($duplicates) { Fail "Duplicate requirement IDs: $((($duplicates | Select-Object -ExpandProperty Name) -join ', '))" }
$allowedScopes = @('v1','post-v1','candidate')
$allowedStatuses = @('IMPLEMENTED-STATIC','IMPLEMENTED-ACCEPTED','PLANNED-V1','POST-V1','NEEDS-CONFIRMATION')
foreach ($row in $reqRows) {
    if ($allowedScopes -notcontains $row.Scope) { Fail "Invalid requirement scope for $($row.Id): $($row.Scope)" }
    if ($allowedStatuses -notcontains $row.Status) { Fail "Invalid requirement status for $($row.Id): $($row.Status)" }
}
$mustHaveIds = @('BT-REL-001','BT-RPT-003','BT-RPT-018','BT-UI-014','BT-BATCH-010','BT-BATCH-011','BT-IMP-010','BT-CORR-001','BT-HIST-002','BT-HIST-003','BT-HIST-004','BT-HIST-005','BT-HIST-006','BT-BIZ-003','BT-COMM-003','BT-DASH-001','BT-OPS-001','BT-UI-009','BT-ARCH-005','BT-ARCH-008','BT-ARCH-009','BT-ARCH-010','BT-ARCH-011','BT-ARCH-012','BT-ARCH-013','BT-ARCH-014','BT-ARCH-015')
foreach ($id in $mustHaveIds) { if (-not ($reqRows.Id -contains $id)) { Fail "Requirements register lost mandatory ID: $id" } }

# Permanent central-service / concurrency portability gate (BT-ARCH-008..015).
$servicesSource = Get-Content -Raw -LiteralPath 'src/BinTracker.Services/Services.cs'
$movementSource = Get-Content -Raw -LiteralPath 'src/BinTracker.Services/MovementServices.cs'
$correctionSource = Get-Content -Raw -LiteralPath 'src/BinTracker.Services/MovementCorrectionService.cs'
$sharedModel = Get-Content -Raw -LiteralPath 'src/BinTracker.Data/BinTrackerDbContext.cs'
foreach ($term in @('IUserContext','IBusinessClock','IClientContext','ConfiguredBusinessClock','DesktopClientContext')) {
    if ($servicesSource -notmatch [regex]::Escape($term)) { Fail "BT-ARCH runtime abstraction missing: $term" }
}
if ($movementSource -notmatch 'IUserContext session' -or $movementSource -notmatch 'IBusinessClock clock' -or $movementSource -notmatch 'IClientContext client') {
    Fail 'BT-ARCH movement service is not request/context portable.'
}
if ($correctionSource -notmatch 'database uniqueness constraint is the authoritative guard' -or $correctionSource -notmatch 'catch \(DbUpdateException\)' -or $correctionSource -notmatch 'already been reversed') {
    Fail 'BT-ARCH concurrent reversal conflict handling is incomplete.'
}
if ($sharedModel -match '\.HasFilter\(' -or $sharedModel -match 'HasCheckConstraint\(') {
    Fail 'BT-ARCH shared EF model contains provider SQL fragments.'
}
foreach ($path in @('src/BinTracker.Services/MovementServices.cs','src/BinTracker.Services/MovementCorrectionService.cs')) {
    $text = Get-Content -Raw -LiteralPath $path
    if ($path -like '*MovementServices.cs') { $text = ($text -split 'internal sealed class MovementService', 2)[1] }
    if ($text -match 'Environment\.MachineName' -or $text -match 'DateTime\.Today' -or $text -match 'DateTime\.UtcNow') {
        Fail "$path bypasses injected client/business-time context."
    }
}

# Reject contradictions that have already caused release/audit drift.
$staleChecks = @(
    @{ Path='docs/Testing.md'; Text='the header reports an 8.0.x SDK'; Why='obsolete SDK-8 build-host assertion' },
    @{ Path='TECH-DEBT.md'; Text='Resolved for the current .NET 8 product line with repository-root `global.json`'; Why='obsolete global.json policy' },
    @{ Path='docs/ImportWizard.md'; Text='still needs the controlled difference/replacement workflow'; Why='Replace/Correct is implemented' },
    @{ Path='docs/ImportWizard.md'; Text='- Import Run history/details UI;'; Why='Import History UI is implemented' },
    @{ Path='TEST-CHECKLIST.md'; Text='exposes Run Report / Today / Export CSV'; Why='interactive reports use live refresh and Customer-on-Enter' },
    @{ Path='TEST-CHECKLIST.md'; Text='Containers remains in Settings pending explicit navigation decision.'; Why='Containers navigation decision is implemented' },
    @{ Path='docs/RELEASE-NOTES.md'; Text='Container Types/Containers remains inside Settings pending an explicit navigation decision.'; Why='Containers navigation decision is implemented' },
    @{ Path='docs/Roadmap.md'; Text='Container Types/Containers left-navigation placement remains a separate pending decision'; Why='Containers navigation decision is implemented' },
    @{ Path='docs/Testing.md'; Text='Containers remains in Settings until an explicit navigation decision is approved.'; Why='Containers navigation decision is implemented' }
)
foreach ($check in $staleChecks) {
    if ((Get-Content -Raw -LiteralPath $check.Path).Contains($check.Text)) { Fail "$($check.Path) retains $($check.Why)." }
}

# Security/Data Integrity/Code Quality hard gate.
$securityRegisterPath = 'docs/SecurityHardeningRegister.md'
if (-not (Test-Path -LiteralPath $securityRegisterPath)) { Fail 'Security hardening finding register is missing.' }
$securityRegister = Get-Content -Raw -LiteralPath $securityRegisterPath
$securityRows = [regex]::Matches($securityRegister, '(?m)^\|\s*(BT-SH-\d{3})\s*\|\s*([A-Z0-9-]+)\s*\|')
if ($securityRows.Count -ne 50) { Fail "Security hardening register must contain exactly the original 50 audit findings; found $($securityRows.Count)." }
$securityIds = @($securityRows | ForEach-Object { $_.Groups[1].Value })
for ($i = 1; $i -le 50; $i++) {
    $id = 'BT-SH-{0:D3}' -f $i
    if ($securityIds -notcontains $id) { Fail "Security hardening register lost finding $id." }
}
$allowedSecurityDispositions = @('CONFIRMED-V1','REVIEW-V1','POST-V1','NOT-APPLICABLE','FIXED')
foreach ($match in $securityRows) {
    $disposition = $match.Groups[2].Value
    if ($allowedSecurityDispositions -notcontains $disposition) { Fail "Invalid security finding disposition for $($match.Groups[1].Value): $disposition" }
}
foreach ($id in @('BT-SEC-008','BT-SEC-009','BT-SEC-010','BT-SEC-011')) {
    if (-not ($reqRows.Id -contains $id)) { Fail "Requirements register lost security hard-gate requirement: $id" }
}

$roadmap = Get-Content -Raw -LiteralPath 'docs/Roadmap.md'
$movementIndex = $roadmap.IndexOf('**Movement Correction / Reversal**')
$hardeningIndex = $roadmap.IndexOf('**Security, Data Integrity & Code Quality Hardening — HARD GATE**')
$brandingIndex = $roadmap.IndexOf('**Business Information & Branding**')
if ($movementIndex -lt 0 -or $hardeningIndex -lt 0 -or $brandingIndex -lt 0 -or -not ($movementIndex -lt $hardeningIndex -and $hardeningIndex -lt $brandingIndex)) {
    Fail 'Roadmap hard gate failed: Security/Data Integrity/Code Quality Hardening must remain after Movement Correction/Reversal and before Branding.'
}
if ($version -match '^1\.0' -and $securityRegister -match '(?m)^\|\s*BT-SH-\d{3}\s*\|\s*(CONFIRMED-V1|REVIEW-V1)\s*\|') {
    Fail 'v1.0 release blocked: unresolved v1 security hardening findings remain.'
}

$requiredRoadmapTerms = @('Movement Correction','Business Information & Branding','Email, SMS','Dashboard','WinUI 3','Daily Print Pack','PostgreSQL','Customer-list-only import mode','Import Profiles')
foreach ($term in $requiredRoadmapTerms) { if ($roadmap -notmatch [regex]::Escape($term)) { Fail "Roadmap lost required workstream/detail: $term" } }

# Source presence for major currently-implemented reporting/audit paths.
$sourceChecks = @(
    @{ Path='src/BinTracker.WinForms/ReportCsvAudit.cs'; Term='ReportCsvAudit'; Desc='CSV audit helper' },
    @{ Path='src/BinTracker.Services/DailyPrintPackService.cs'; Term='DAILY_PRINT_PACK_GENERATED'; Desc='Daily Print Pack audit' },
    @{ Path='src/BinTracker.WinForms/SplashForm.cs'; Term='Splash'; Desc='startup splash' }
)
foreach ($check in $sourceChecks) {
    if (-not (Test-Path -LiteralPath $check.Path)) { Fail "Missing source for $($check.Desc): $($check.Path)" }
    if ((Get-Content -Raw -LiteralPath $check.Path) -notmatch [regex]::Escape($check.Term)) { Fail "$($check.Desc) source check failed." }
}

$csvEventChecks = @(
    @{ Path='src/BinTracker.WinForms/OutstandingContainersReportForm.cs'; Event='OUTSTANDING_CONTAINERS_CSV_EXPORTED' },
    @{ Path='src/BinTracker.WinForms/DailyMovementsReportForm.cs'; Event='DAILY_MOVEMENTS_CSV_EXPORTED' },
    @{ Path='src/BinTracker.WinForms/WeeklyMovementsReportForm.cs'; Event='WEEKLY_MOVEMENTS_CSV_EXPORTED' },
    @{ Path='src/BinTracker.WinForms/MovementHistoryReportForm.cs'; Event='MOVEMENT_HISTORY_CSV_EXPORTED' },
    @{ Path='src/BinTracker.WinForms/MonthlySummaryReportForm.cs'; Event='MONTHLY_SUMMARY_CSV_EXPORTED' }
)
foreach ($check in $csvEventChecks) {
    if (-not (Test-Path -LiteralPath $check.Path)) { Fail "Missing CSV-capable report form: $($check.Path)" }
    if ((Get-Content -Raw -LiteralPath $check.Path) -notmatch [regex]::Escape($check.Event)) { Fail "$($check.Path) lost CSV audit event $($check.Event)." }
}

# Containers navigation/permission compromise (BT-CT-005 / BT-UI-012) must remain mechanically represented in source.
$mainForm = Get-Content -Raw -LiteralPath 'src/BinTracker.WinForms/MainForm.cs'
$containerForm = Get-Content -Raw -LiteralPath 'src/BinTracker.WinForms/ContainerTypesForm.cs'
if ($mainForm -notmatch 'Nav\("nav_containers",\s*"Containers",\s*ShowContainers\)') { Fail 'Containers left-navigation destination is missing.' }
if ($mainForm -notmatch 'canEdit:\s*session\.Role\s*==\s*UserRole\.Administrator') { Fail 'Containers administrator edit gate is missing.' }
if ($mainForm -notmatch 'Container Types are managed from the Containers navigation page') { Fail 'Settings still lacks the expected Containers handoff text.' }
if ($containerForm -notmatch 'View only — administrator access is required to add or change container types\.') { Fail 'Containers non-admin read-only state is missing.' }
if ($containerForm -notmatch 'add\.Visible\s*=\s*canEdit' -or $containerForm -notmatch 'save\.Visible\s*=\s*canEdit' -or $containerForm -notmatch 'deactivate\.Visible\s*=\s*canEdit') { Fail 'Containers mutation controls are not hidden for read-only users.' }
if ($containerForm -notmatch 'public\s+Task<bool>\s+ConfirmCanLeaveAsync') { Fail 'Containers unsaved-change navigation protection is missing.' }

$mdFiles = @(Get-ChildItem -Recurse -File -Filter '*.md')
if (-not ($reqRows.Id -contains "BT-CT-005")) {
    Fail "Requirements register is missing mandatory requirement BT-CT-005."
}
if (-not ($reqRows.Id -contains "BT-UI-013")) {
    Fail "Requirements register is missing mandatory requirement BT-UI-013."
}
if (-not ($reqRows.Id -contains "BT-RPT-011")) {
    Fail "Requirements register is missing mandatory requirement BT-RPT-011."
}
if (-not ($reqRows.Id -contains "BT-RPT-012")) {
    Fail "Requirements register is missing mandatory requirement BT-RPT-012."
}
if (-not ($reqRows.Id -contains "BT-RPT-013")) {
    Fail "Requirements register is missing mandatory requirement BT-RPT-013."
}

if (-not ($reqRows.Id -contains "BT-RPT-014")) {
    Fail "Requirements register is missing mandatory requirement BT-RPT-014."
}
if (-not ($reqRows.Id -contains "BT-RPT-015")) {
    Fail "Requirements register is missing mandatory requirement BT-RPT-015."
}
if (-not ($reqRows.Id -contains "BT-RPT-016")) {
    Fail "Requirements register is missing mandatory requirement BT-RPT-016."
}
if (-not ($reqRows.Id -contains "BT-RPT-017")) {
    Fail "Requirements register is missing mandatory requirement BT-RPT-017."
}

$outstandingService = Get-Content -Raw (Join-Path $root "src\BinTracker.Services\OutstandingReportService.cs")
$outstandingForm = Get-Content -Raw (Join-Path $root "src\BinTracker.WinForms\OutstandingContainersReportForm.cs")
if ($outstandingService -notmatch "CreditsOnly" -or $outstandingService -notmatch "AllNonZero" -or $outstandingForm -notmatch '"Credits only"' -or $outstandingForm -notmatch '"All non-zero"') {
    Fail "BT-RPT-014 source gate failed: Outstanding Containers balance modes are incomplete."
}
if ($outstandingForm -notmatch 'Width\s*=\s*215' -or $outstandingForm -notmatch 'ReportGridMultiSort\.Wrap\(grid\)') {
    Fail "BT-RPT-015 source gate failed: Outstanding Containers DPI-safe balance selector or approved multi-column sort is incomplete."
}


# Shared report multi-column sorting/hint + Outstanding control visibility guard (BT-RPT-016).
$multiSortPath = Join-Path $root "src\BinTracker.WinForms\ReportGridMultiSort.cs"
$outstandingPath = Join-Path $root "src\BinTracker.WinForms\OutstandingContainersReportForm.cs"
$multiSortText = Get-Content -LiteralPath $multiSortPath -Raw
$outstandingText = Get-Content -LiteralPath $outstandingPath -Raw
if ($multiSortText -notmatch 'Shift\+click to add another column' -or
    $multiSortText -notmatch 'ColumnHeaderMouseClick' -or
    $multiSortText -notmatch 'TryDecimal' -or
    $multiSortText -notmatch 'LeadingNumber' -or
    $multiSortText -notmatch 'TryDate' -or
    $multiSortText -notmatch 'SetTypedSortValue' -or
    $multiSortText -notmatch 'Contains\("CREDIT"' -or
    $multiSortText -notmatch 'public static void Reapply' -or
    $outstandingText -notmatch 'SetTypedSortValue\(' -or
    $outstandingText -notmatch '"Position"' -or
    $outstandingText -notmatch 'OutstandingReportRow\)\?\.Balance' -or
    $outstandingText -match 'OutstandingGridComparer' -or
    $outstandingText -match 'Grid_ColumnHeaderMouseClick' -or
    $outstandingText -notmatch 'ReportGridMultiSort\.Reapply\(grid\)' -or
    $outstandingText -notmatch 'controlRows.Controls.Add\(actions, 0, 2\)') {
    Fail "BT-RPT-016 source gate failed: type-aware shared report multi-sort/hint, refresh persistence, or Outstanding action-row visibility is incomplete."
}

# Visible report sort direction/priority guard (BT-RPT-017).
$dailyMovementsText = Get-Content -Raw (Join-Path $root "src\BinTracker.WinForms\DailyMovementsReportForm.cs")
$weeklyMovementsText = Get-Content -Raw (Join-Path $root "src\BinTracker.WinForms\WeeklyMovementsReportForm.cs")
if ($multiSortText -notmatch 'var indicator = item.Direction == SortOrder.Descending' -or
    $multiSortText -notmatch 'HeaderText = \$"\{baseText\} \{indicator\}\{i \+ 1\}"' -or
    $multiSortText -notmatch 'ReserveSortIndicatorSpace' -or
    $multiSortText -notmatch 'HeaderCell\.Style\.WrapMode = DataGridViewTriState\.False' -or
    $dailyMovementsText -notmatch 'Column\("Direction", 125, "Direction"\)' -or
    $weeklyMovementsText -notmatch 'Column\("Direction",120,"Direction"\)') {
    Fail "BT-RPT-017 source gate failed: explicit visible sort direction/priority, single-line active headers, stable sort widths, or DPI-safe Direction header width is incomplete."
}

# Movement correction/reversal hard source gate (BT-CORR-001..004).
$correctionServicePath = Join-Path $root "src\BinTracker.Services\MovementCorrectionService.cs"
$correctionDialogPath = Join-Path $root "src\BinTracker.WinForms\MovementReversalDialog.cs"
$movementHistoryPath = Join-Path $root "src\BinTracker.WinForms\MovementHistoryReportForm.cs"
$migrationPath = Join-Path $root "src\BinTracker.Data\SqliteSchemaMigrations.cs"
if (-not (Test-Path $correctionServicePath) -or -not (Test-Path $correctionDialogPath)) {
    Fail "BT-CORR source gate failed: correction service/dialog missing."
}
$correctionText = Get-Content -Raw $correctionServicePath
$correctionDialogText = Get-Content -Raw $correctionDialogPath
$correctionUiText = Get-Content -Raw $movementHistoryPath
$migrationText = Get-Content -Raw $migrationPath
if ($correctionText -notmatch 'session\.Role is not \(UserRole\.Administrator or UserRole\.Operator\)' -or
    $correctionText -notmatch 'original\.Source == MovementSource\.Adjustment' -or
    $correctionText -notmatch 'original\.Source == MovementSource\.ExcelImport \|\| original\.ImportRunId\.HasValue' -or
    $correctionText -notmatch 'Replace / Correct' -or
    $correctionText -notmatch 'BeginTransactionAsync' -or
    $correctionText -notmatch 'ReversesMovementId = original\.Id' -or
    $correctionText -notmatch 'MOVEMENT_REVERSED' -or
    $correctionUiText -notmatch 'session\.Role is UserRole\.Administrator or UserRole\.Operator' -or
    $correctionUiText -notmatch 'Reverse Selected' -or
    $correctionDialogText -notmatch 'ClientSize = new Size\(620, 540\)' -or
    $correctionDialogText -notmatch 'SizeType\.Absolute, 130F' -or
    $correctionDialogText -notmatch 'SizeType\.Absolute, 54F' -or
    $correctionDialogText -notmatch 'AutoSize = false' -or
    $correctionDialogText -notmatch 'MinimumSize = new Size\(0, 96\)' -or
    $migrationText -notmatch 'new\(13, "Movement correction and reversal linkage"') {
    Fail "BT-CORR source gate failed: append-only linked/audited role-sensitive reversal implementation is incomplete."
}

# Batch Entry acceptance/recovery source guard (BT-BATCH-008/009/010/011).
$batchViewText = Get-Content -Raw (Join-Path $root "src\BinTracker.WinForms\BatchEntryView.cs")
$movementServicesText = Get-Content -Raw (Join-Path $root "src\BinTracker.Services\MovementServices.cs")
if ($batchViewText -notmatch 'suppressPendingSelectionChanged = true' -or $batchViewText -notmatch 'pending\.CurrentCell = null') { Fail 'BT-BATCH-006 source gate failed: edit reset must suppress pending selection and clear CurrentCell.' }
$batchStoreText = Get-Content -Raw (Join-Path $root "src\BinTracker.Services\FileBatchDraftStore.cs")
$mainFormText = Get-Content -Raw (Join-Path $root "src\BinTracker.WinForms\MainForm.cs")
if ($batchViewText -notmatch 'HasCurrentLineInput' -or
    $batchViewText -notmatch 'ClearCurrentLineEntry' -or
    $batchViewText -notmatch 'exitRequested' -or
    $batchViewText -notmatch 'appState\.PersistDraft\(\)' -or
    $batchViewText -notmatch 'appState\.ClearDraft\(\)' -or
    $batchViewText -notmatch 'clearContainer:\s*false' -or
    $batchViewText -notmatch 'suppressPendingSelectionChanged' -or
    $batchViewText -notmatch 'editLoadGeneration' -or
    $batchViewText -notmatch 'generation != editLoadGeneration' -or
    $batchViewText -notmatch 'pending\.CurrentCell = null' -or
    $batchViewText -notmatch 'ClearCurrentLineEntry\(clearContainer:\s*false\)' -or
    $batchViewText -notmatch 'SubmitCurrentLineAsync' -or
    $batchViewText -notmatch 'editingLine is null \? AddLineAsync\(\) : UpdateLineAsync\(\)' -or
    $batchViewText -notmatch 'bar\.Controls\.Add\(status, 0, 1\)' -or
    $movementServicesText -notmatch 'IBatchDraftStore' -or
    $movementServicesText -notmatch 'PersistDraft' -or
    $movementServicesText -notmatch 'RecoveryPromptPending' -or
    $movementServicesText -notmatch 'RecoveryDraftLastSavedAtUtc' -or
    $movementServicesText -notmatch 'MarkRecoveryPromptHandled' -or
    $batchStoreText -notmatch 'LocalApplicationData' -or
    $batchStoreText -notmatch 'batch-entry-draft\.json' -or
    $batchStoreText -notmatch 'SavedAtUtc' -or
    $batchStoreText -notmatch 'File\.Move\(temporaryPath, filePath, overwrite:\s*true\)' -or
    $mainFormText -notmatch 'new BatchEntryView\(movements, session, appState, ShowDashboard\)' -or
    $mainFormText -notmatch 'HandleRecoveredBatchAsync' -or
    $mainFormText -notmatch 'SaveRecoveredBatchAsync' -or
    $mainFormText -notmatch 'RecoveredBatchAction\.Continue' -or
    $mainFormText -notmatch 'RecoveredBatchAction\.Save' -or
    $mainFormText -notmatch 'RecoveredBatchAction\.Discard' -or
    $mainFormText -notmatch 'SelectNavigationForPage\(page\)') {
    Fail "Batch Entry source gate failed: BT-BATCH-008/009/010/011 Esc/reset/recovery implementation is incomplete."
}
$recoveredDialogText = Get-Content -Raw (Join-Path $root "src\BinTracker.WinForms\RecoveredBatchDialog.cs")
if ($recoveredDialogText -notmatch 'Continue Batch' -or
    $recoveredDialogText -notmatch 'Save Batch' -or
    $recoveredDialogText -notmatch 'Discard Batch' -or
    $recoveredDialogText -notmatch 'Pending lines' -or
    $recoveredDialogText -notmatch 'Total containers' -or
    $recoveredDialogText -notmatch 'Last saved' -or
    $recoveredDialogText -notmatch 'TableLayoutPanel' -or
    $recoveredDialogText -notmatch 'TextAlign = ContentAlignment\.MiddleCenter' -or
    $recoveredDialogText -notmatch 'FlatStyle = FlatStyle\.System' -or
    $recoveredDialogText -notmatch 'Padding = Padding\.Empty' -or
    $recoveredDialogText -notmatch 'UseCompatibleTextRendering = false' -or
    $recoveredDialogText -notmatch 'UseVisualStyleBackColor = true' -or
    $recoveredDialogText -notmatch 'Dock = DockStyle\.Fill') {
    Fail "BT-BATCH-011 source gate failed: recovered-batch decision dialog/alignment is incomplete."
}

# Reports landing-page viewport/icon guard (BT-RPT-012).
$reportsView = Get-Content -Raw -LiteralPath 'src/BinTracker.WinForms/ReportsView.cs'
if ($reportsView -notmatch 'AutoScroll\s*=\s*false') { Fail 'ReportsView itself must remain non-scrollable at the normal landing-page viewport.' }
if ($reportsView -match 'var\s+scrollHost\s*=\s*new\s+Panel' -or $reportsView -match 'FitRootToViewport') { Fail 'Reports landing page reverted to the scrollbar/viewport-compensation approach.' }
if ($reportsView -notmatch 'var\s+root\s*=\s*new\s+TableLayoutPanel' -or $reportsView -notmatch 'Dock\s*=\s*DockStyle\.Fill') { Fail 'Reports landing page lost its viewport-filling root layout.' }
if ($reportsView -notmatch 'ConfigureActionButton' -or $reportsView -notmatch 'TextFormatFlags\.SingleLine' -or $reportsView -notmatch 'TextFormatFlags\.NoPrefix') { Fail 'Reports action buttons lost their single-line custom caption rendering.' }
if ($reportsView -notmatch 'CreateDocumentIcon' -or $reportsView -notmatch 'CreateExternalLinkIcon') { Fail 'Reports action buttons lost their reliable drawn document/external-link icons.' }
if ($reportsView -match 'PrimaryActionButton\("↗') { Fail 'Reports action buttons reverted to a font-dependent arrow glyph.' }
if ($reportsView -match 'InformationBar\s*\(' -or $reportsView -match 'All reports can be exported to PDF and CSV') { Fail 'Reports landing page regained the redundant bottom information bar.' }

# Every report Container Type selector must use configured master data, including
# inactive types for historical filtering. Do not derive choices from current
# outstanding balances.
$reportFilterForms = @(
    'src/BinTracker.WinForms/OutstandingContainersReportForm.cs',
    'src/BinTracker.WinForms/DailyMovementsReportForm.cs',
    'src/BinTracker.WinForms/WeeklyMovementsReportForm.cs',
    'src/BinTracker.WinForms/MovementHistoryReportForm.cs',
    'src/BinTracker.WinForms/MonthlySummaryReportForm.cs'
)
foreach ($path in $reportFilterForms) {
    $source = Get-Content -Raw -LiteralPath $path
    if ($source -notmatch 'containerTypes\.SearchAsync\(') { Fail "$path no longer uses configured Container Types for its report filter." }
    if ($source -notmatch 'includeInactive:\s*true') { Fail "$path no longer keeps inactive historical Container Types filterable." }
}

# BT-ARCH-008..015: central-service/PostgreSQL portability and concurrency hard gate.
$productionLayers = @('src/BinTracker.Core','src/BinTracker.Services','src/BinTracker.WinForms')
$providerPatterns = @('Microsoft\.Data\.Sqlite','\bUseSqlite\b','\bSqliteConnection\b','\bPRAGMA\b','\bExecuteSqlRaw','\bSqlQueryRaw')
foreach ($layer in $productionLayers) {
    Get-ChildItem -LiteralPath (Join-Path $root $layer) -Recurse -Filter '*.cs' | ForEach-Object {
        $source = Get-Content -Raw -LiteralPath $_.FullName
        foreach ($pattern in $providerPatterns) {
            if ($source -match $pattern) { Fail "BT-ARCH-009 provider isolation failed: $($_.FullName) matches '$pattern'." }
        }
    }
}

Get-ChildItem -LiteralPath (Join-Path $root 'src/BinTracker.Services') -Recurse -Filter '*.cs' | ForEach-Object {
    if ($_.Name -in @('RuntimeContexts.cs','FileBatchDraftStore.cs')) { return }
    $source = Get-Content -Raw -LiteralPath $_.FullName
    if ($source -match '\bDateTime\.(Today|Now|UtcNow)\b' -or $source -match '\bEnvironment\.MachineName\b') {
        Fail "BT-ARCH-010 runtime context failed: $($_.FullName) uses ambient business context."
    }
    if ($source -match '\bFile\.(Read|Write|Open|Exists|Delete|Move)' -or $source -match '\bFileStream\b') {
        Fail "BT-ARCH-011 transport failed: $($_.FullName) performs client filesystem I/O."
    }
    if ($source -match '\bSourceFullPath\b') { Fail "BT-ARCH-011 transport metadata failed: $($_.FullName) retains SourceFullPath." }
}

$runtimeContextText = Get-Content -Raw 'src/BinTracker.Services/RuntimeContexts.cs'
$registrationText = Get-Content -Raw 'src/BinTracker.Services/Services.cs'
$importExecutionText = Get-Content -Raw 'src/BinTracker.Services/ImportExecutionService.cs'
$importAnalysisText = Get-Content -Raw 'src/BinTracker.Services/ExcelImportService.cs'
$containerServiceText = Get-Content -Raw 'src/BinTracker.Services/ContainerTypeService.cs'
$customerServiceText = Get-Content -Raw 'src/BinTracker.Services/CustomerServices.cs'
$businessInfoText = Get-Content -Raw 'src/BinTracker.Services/BusinessInformationService.cs'

foreach ($term in @('interface IUserContext','interface IBusinessClock','interface IClientContext')) {
    if ($runtimeContextText -notmatch [regex]::Escape($term)) { Fail "BT-ARCH-010 missing $term." }
}
if ($registrationText -notmatch 'AddBinTrackerBusinessServices' -or $registrationText -notmatch 'AddBinTrackerServices' -or
    $registrationText -notmatch 'AddSingleton<IUserContext>') {
    Fail 'BT-ARCH-010 host composition split is incomplete.'
}
if ($registrationText -notmatch 'ExecuteUpdateAsync' -or $registrationText -notmatch 'FailedLoginCount \+ 1' -or
    $registrationText -notmatch 'PasswordHash == user\.PasswordHash') {
    Fail 'BT-ARCH-014 authentication mutations are not concurrency-safe.'
}
if ($movementSource -notmatch 'Guid ClientOperationId' -or $correctionSource -notmatch 'Guid ClientOperationId' -or
    $importExecutionText -notmatch 'Guid ClientOperationId' -or $importExecutionText -notmatch 'ClientRequestFingerprint' -or
    $importExecutionText -notmatch 'BuildRequestFingerprint') {
    Fail 'BT-ARCH-012 payload-aware command idempotency is incomplete.'
}
if ($sharedModel -notmatch 'HasIndex\(x => x\.NameKey\)\.IsUnique\(\)' -or
    $sharedModel -notmatch 'HasIndex\(x => x\.CurrentCutoverDate\)\.IsUnique\(\)' -or
    $sharedModel -notmatch 'IsConcurrencyToken\(\)' -or
    $containerServiceText -notmatch 'ContainerTypeNameKey\.Normalize' -or
    $customerServiceText -notmatch 'DbUpdateConcurrencyException' -or
    $businessInfoText -notmatch 'DbUpdateConcurrencyException') {
    Fail 'BT-ARCH-013/014 database-backed uniqueness or stale-edit protection is incomplete.'
}
if ($importAnalysisText -notmatch 'record ImportSourceDocument' -or $importAnalysisText -notmatch 'byte\[\] Content' -or
    $importExecutionText -notmatch 'SourceClientPath = fingerprint\.ClientPath' -or
    $importExecutionText -notmatch 'CurrentCutoverDate = request\.CutoverDate') {
    Fail 'BT-ARCH-011/014 import transport or current-cutover ownership is incomplete.'
}
if ($migrationText -notmatch 'new\(14, "Multi-user portability and concurrency foundation"' -or
    $migrationText -notmatch 'SourceClientPath' -or $migrationText -notmatch 'IX_ContainerTypes_NameKey' -or
    $migrationText -notmatch 'ClientRequestFingerprint') {
    Fail 'BT-ARCH-015 schema migration gate is incomplete.'
}

$movementHistoryServiceText = Get-Content -Raw 'src/BinTracker.Services/MovementHistoryReportService.cs'
if ($movementHistoryServiceText -notmatch 'Reversed — see' -or $movementHistoryServiceText -notmatch 'Reversal of #' -or
    $movementHistoryServiceText -notmatch 'SourceText => ReversesMovementId\.HasValue \? "Reversal"' -or
    $correctionUiText -notmatch 'UpdateReverseAvailability' -or $correctionUiText -notmatch 'CanReverse') {
    Fail 'Movement History reversal Status/Source and disabled-action UX is incomplete.'
}

# Integrated Movement History presentation/export gate (BT-HIST-002..006).
$movementHistoryFileNameText = Get-Content -Raw 'src/BinTracker.Services/MovementHistoryExportFileName.cs'
if ($mainFormText -notmatch 'SetPage\(\s*"Movement History"' -or
    $mainFormText -notmatch 'TopLevel = false' -or
    $mainFormText -notmatch 'FormBorderStyle = FormBorderStyle\.None' -or
    $mainFormText -notmatch 'activeMovementHistoryPage\.Show\(\)' -or
    $mainFormText -notmatch 'reportsBreadcrumbLink\.Text = "Reports"' -or
    $mainFormText -notmatch 'breadcrumbSeparator\.Text = "›"' -or
    $mainFormText -notmatch 'pageBreadcrumb\.Visible = showReportsBreadcrumb' -or
    $mainFormText -notmatch 'showReportsBreadcrumb: true' -or
    $correctionUiText -notmatch 'RowCount = 5' -or
    $correctionUiText -notmatch 'root\.Controls\.Add\(filters, 0, 0\)' -or
    $correctionUiText -notmatch 'root\.Controls\.Add\(options, 0, 1\)' -or
    $correctionUiText -notmatch 'root\.Controls\.Add\(actions, 0, 2\)' -or
    $correctionUiText -notmatch 'root\.Controls\.Add\(summaryCard, 0, 3\)' -or
    $correctionUiText -notmatch 'root\.Controls\.Add\(gridCard, 0, 4\)' -or
    $correctionUiText -match '← Reports' -or
    $correctionUiText -match 'controlsCard' -or
    $correctionUiText -notmatch 'actions\.MinimumSize = new Size\(0, 52\)' -or
    $correctionUiText -notmatch 'actions\.WrapContents = true' -or
    $correctionUiText -notmatch 'FilterGroup' -or
    $correctionUiText -notmatch 'AllocateResponsiveColumns' -or
    $correctionUiText -notmatch 'available < minimumTotal' -or
    $correctionUiText -notmatch 'grid\.Resize' -or
    $correctionUiText -notmatch 'CellPainting' -or
    $correctionUiText -notmatch 'Color\.FromArgb\(218, 242, 226\)' -or
    $correctionUiText -notmatch 'Color\.FromArgb\(250, 222, 222\)' -or
    $correctionUiText -notmatch 'Color\.FromArgb\(255, 232, 194\)' -or
    $correctionUiText -notmatch 'Cells\["Status"\]\.ToolTipText' -or
    $correctionUiText -notmatch 'Cells\["Notes"\]\.ToolTipText' -or
    $correctionUiText -notmatch 'MovementHistoryExportFileName\.Build' -or
    $movementHistoryFileNameText -notmatch 'ResolveSingleCustomerCode' -or
    $movementHistoryFileNameText -notmatch 'SanitizeWindowsSegment' -or
    $correctionUiText -notmatch 'Reference,Status' -or
    (Get-Content -Raw 'src/BinTracker.Services/MovementHistoryReportPdfService.cs') -notmatch 'Header\(table, "Status"\)') {
    Fail 'BT-HIST-002..006 source gate failed: integrated layout, responsive columns, badges/tooltips or stable customer-code export naming is incomplete.'
}

Write-Host "Audit passed: $expected; $($reqRows.Count) permanent requirement IDs; $($mdFiles.Count) Markdown files; current-state contradiction checks passed." -ForegroundColor Green
exit 0
