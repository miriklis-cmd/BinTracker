param(
    [string]$RoadmapPath = 'docs/Roadmap.md',
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'

function Assert-RoadmapOrder([string]$Text) {
    $featureHeading = '### Post-hardening v1 features'
    $releaseHeading = '### Release closure'
    if ([regex]::Matches($Text, '(?m)^### Post-hardening v1 features\s*$').Count -ne 1 -or
        [regex]::Matches($Text, '(?m)^### Release closure\s*$').Count -ne 1) {
        throw 'Roadmap must contain one operative feature section and one release-closure section.'
    }
    $featureStart = $Text.IndexOf($featureHeading, [StringComparison]::Ordinal)
    $releaseStart = $Text.IndexOf($releaseHeading, [StringComparison]::Ordinal)
    if ($releaseStart -le $featureStart) { throw 'Release closure must follow the operative features.' }

    # Keep the completed hardening sequence explicit without using later feature text as its anchor.
    $history = $Text.Substring(0, $featureStart)
    $movement = $history.IndexOf('**Movement Correction / Reversal**', [StringComparison]::Ordinal)
    $hardening = [regex]::Match($history, '\*\*Security, Data Integrity & Code Quality Hardening [^*\r\n]*HARD GATE\*\*')
    if ($movement -lt 0 -or -not $hardening.Success -or $hardening.Index -le $movement) {
        throw 'Historical Movement Correction and Security Hardening sequence is missing or reversed.'
    }

    $features = $Text.Substring($featureStart, $releaseStart - $featureStart)
    $items = [regex]::Matches($features, '(?m)^(\d+)\. \*\*([^*\r\n]+)\*\*([^\r\n]*)')
    $expected = @('Email, SMS & Customer Communications', 'Deterministic configurable Container Type ordering',
        'Dashboard', 'Business Information & Branding', 'Customer operational analytics/polish',
        'Production Backup / Restore / Recovery')
    if ($items.Count -ne $expected.Count) { throw 'Operative roadmap must contain exactly six feature milestones.' }
    for ($i = 0; $i -lt $expected.Count; $i++) {
        if ($items[$i].Groups[1].Value -cne [string]($i + 1) -or
            $items[$i].Groups[2].Value -cne $expected[$i]) {
            throw "Operative roadmap feature order differs at position $($i + 1)."
        }
    }
    if ($items[2].Groups[3].Value -notmatch 'mandatory design gate before implementation') {
        throw 'Dashboard must require the design gate before implementation.'
    }
    $designGate = [regex]::Match($Text, '(?ms)^### Dashboard milestone design gate\s*\r?\n(?<body>.*?)(?=^## )')
    if (-not $designGate.Success -or $designGate.Groups['body'].Value -notmatch 'Only implement after a preferred direction is agreed\.') {
        throw 'Dashboard design gate must require an agreed direction before implementation.'
    }
    $release = $Text.Substring($releaseStart) -split '(?m)^## ', 2 | Select-Object -First 1
    if ($release -notmatch 'full v1 regression and acceptance, and v1\.0 release') {
        throw 'Final v1 acceptance and release closure is missing.'
    }
}

$roadmap = Get-Content -Raw -Encoding UTF8 -LiteralPath $RoadmapPath
Assert-RoadmapOrder $roadmap
if ($SelfTest) {
    $badOrder = $roadmap.Replace('3. **Dashboard**', '3. **Business Information & Branding**').Replace('4. **Business Information & Branding**', '4. **Dashboard**')
    $rejected = $false
    try { Assert-RoadmapOrder $badOrder } catch { $rejected = $true }
    if (-not $rejected) { throw 'Self-test failed: incorrect operative order passed.' }
    $rejected = $false
    try { Assert-RoadmapOrder ($roadmap.Replace('mandatory design gate before implementation', 'optional design discussion')) } catch { $rejected = $true }
    if (-not $rejected) { throw 'Self-test failed: missing Dashboard design gate passed.' }
    $rejected = $false
    try { Assert-RoadmapOrder ($roadmap.Replace('Only implement after a preferred direction is agreed.', 'Implementation may proceed without agreement.')) } catch { $rejected = $true }
    if (-not $rejected) { throw 'Self-test failed: missing Dashboard design approval passed.' }
    $rejected = $false
    try { Assert-RoadmapOrder ($roadmap.Replace('full v1 regression and acceptance, and v1.0 release', 'later release')) } catch { $rejected = $true }
    if (-not $rejected) { throw 'Self-test failed: missing final acceptance and release passed.' }
    Write-Output 'Roadmap order self-test passed: valid order, swapped milestones, missing design gate, missing approval and missing release closure.'
    return
}
Write-Output 'Operative roadmap order and historical hardening sequence verified.'
