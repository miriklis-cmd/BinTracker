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
)
foreach ($check in $currentChecks) {
    if (-not (Test-Path -LiteralPath $check.Path)) { Fail "Missing $($check.Path)." }
    $content = Get-Content -Raw -LiteralPath $check.Path
    if ($content -notmatch "(?m)$($check.Pattern)") { Fail "$($check.Description) does not match $expected." }
}

if (Test-Path -LiteralPath 'global.json') { Fail 'Unexpected global.json is present. BinTracker currently uses the installed compatible SDK.' }

$requiredDocuments = @(
 'README.md','KNOWN-ISSUES.md','TECH-DEBT.md','TEST-CHECKLIST.md',
 'docs/AuditCoverage.md','docs/BusinessRules.md','docs/CHANGELOG.md','docs/Database.md',
 'docs/DevelopmentWorkflow.md','docs/DocumentationAudit.md','docs/FunctionalSpecification.md',
 'docs/ImportWizard.md','docs/LegacyContainerRules.md','docs/MasterData.md','docs/RELEASE-NOTES.md',
 'docs/ReimportSafety.md','docs/Roadmap.md','docs/RoadmapCoverageMatrix.md','docs/Testing.md',
 'docs/Versioning.md','docs/RequirementsAcceptanceRegister.md','docs/ReconciliationReport.md'
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
$mustHaveIds = @('BT-REL-001','BT-RPT-003','BT-BATCH-010','BT-IMP-010','BT-CORR-001','BT-BIZ-003','BT-COMM-003','BT-DASH-001','BT-OPS-001','BT-UI-009','BT-ARCH-005')
foreach ($id in $mustHaveIds) { if (-not ($reqRows.Id -contains $id)) { Fail "Requirements register lost mandatory ID: $id" } }

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

$roadmap = Get-Content -Raw -LiteralPath 'docs/Roadmap.md'
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

Write-Host "Audit passed: $expected; $($reqRows.Count) permanent requirement IDs; $($mdFiles.Count) Markdown files; current-state contradiction checks passed." -ForegroundColor Green
exit 0
