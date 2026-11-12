param(
    [string]$RegisterPath = 'docs/SecurityHardeningRegister.md',
    [string]$BaselinePath = 'docs/SecurityFixedEvidenceBaseline.json',
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
function Assert-FixedEvidence([string[]]$Rows, [string[]]$Reviewed) {
    $fixed = @($Rows | Where-Object { $_ -cmatch '^\|\s*BT-SH-\d{3}\s*\|\s*FIXED\s*\|' })
    if ($fixed.Count -ne $Reviewed.Count) { throw 'FIXED finding set changed: independent source/test evidence review and baseline update required.' }
    for ($i = 0; $i -lt $fixed.Count; $i++) {
        if (-not [string]::Equals($fixed[$i], $Reviewed[$i], [StringComparison]::Ordinal)) {
            throw "FIXED finding or evidence changed at entry $($i + 1): independent source/test review required."
        }
    }
}
if ($SelfTest) {
    $reviewed = @('| BT-SH-001 | FIXED | Finding | Existing reviewed evidence |')
    Assert-FixedEvidence $reviewed $reviewed
    $rejected = $false
    try { Assert-FixedEvidence ($reviewed + '| BT-SH-002 | FIXED | New | Unsupported |') $reviewed } catch { $rejected = $true }
    if (-not $rejected) { throw 'Unsupported FIXED finding was accepted.' }
    Write-Output 'Security evidence self-test passed: reviewed FIXED and unsupported new FIXED.'
    return
}
$rows = @(Get-Content -Encoding UTF8 -LiteralPath $RegisterPath)
$reviewed = Get-Content -Raw -Encoding UTF8 -LiteralPath $BaselinePath | ConvertFrom-Json
Assert-FixedEvidence $rows $reviewed
Write-Output 'Security FIXED evidence baseline verified; independent review remains required for updates.'
