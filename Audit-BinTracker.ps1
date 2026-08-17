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
    @{ Path='TEST-CHECKLIST.md'; Text='exposes Run Report / Today / Export CSV'; Why='interactive reports use live refresh and Customer-on-Enter' }
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

$mdFiles = @(Get-ChildItem -Recurse -File -Filter '*.md')
Write-Host "Audit passed: $expected; $($reqRows.Count) permanent requirement IDs; $($mdFiles.Count) Markdown files; current-state contradiction checks passed." -ForegroundColor Green
exit 0
