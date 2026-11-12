param(
    [string]$SpecificationPath = 'docs/FunctionalSpecification.md',
    [string]$RegisterPath = 'docs/RequirementsAcceptanceRegister.md',
    [string]$BaselinePath = 'docs/SpecificationRequirementBaseline.json',
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'

function Get-IdentitySnapshot([string[]]$Specification, [string[]]$Register) {
    $definitions = @($Specification | Where-Object { $_ -cmatch '^\s*-\s+BT-[A-Z0-9-]+:' })
    $specIds = @($definitions | ForEach-Object { if ($_ -cmatch '^\s*-\s+(BT-[A-Z0-9-]+):') { $Matches[1] } })
    $collisions = @($Register | Where-Object {
        $_ -cmatch '^\|\s*(BT-[A-Z0-9-]+)\s*\|' -and $specIds -ccontains $Matches[1]
    })
    return [pscustomobject]@{ Definitions = $definitions; RegisterOverlaps = $collisions }
}

function Assert-IdentitySnapshot($Actual, $Approved) {
    foreach ($name in @('Definitions', 'RegisterOverlaps')) {
        $actualLines = @($Actual.$name)
        $approvedLines = @($Approved.$name)
        if ($actualLines.Count -ne $approvedLines.Count) { throw "Specification identity $name count changed; review and approve the mapping." }
        for ($i = 0; $i -lt $actualLines.Count; $i++) {
            if (-not [string]::Equals($actualLines[$i], $approvedLines[$i], [StringComparison]::Ordinal)) {
                throw "Specification identity $name changed at entry $($i + 1); review and approve the mapping."
            }
        }
    }
}

if ($SelfTest) {
    $spec = @('- BT-CUST-001: Legacy definition.', '- BT-NEW-001: Independent clause.')
    $register = @('| BT-CUST-001 | v1 | PLANNED-V1 | Different canonical definition. |')
    $baseline = Get-IdentitySnapshot $spec $register
    Assert-IdentitySnapshot (Get-IdentitySnapshot $spec $register) $baseline
    $failed = $false
    try { Assert-IdentitySnapshot (Get-IdentitySnapshot $spec ($register + '| BT-NEW-001 | v1 | PLANNED-V1 | New collision. |')) $baseline } catch { $failed = $true }
    if (-not $failed) { throw 'Self-test failed: new cross-document collision was accepted.' }
    $failed = $false
    try { Assert-IdentitySnapshot (Get-IdentitySnapshot @('- BT-CUST-001: Altered.', $spec[1]) $register) $baseline } catch { $failed = $true }
    if (-not $failed) { throw 'Self-test failed: altered legacy definition was accepted.' }
    Write-Output 'Specification identity self-test passed: approved legacy overlap, new collision and altered definition.'
    return
}

$actual = Get-IdentitySnapshot @(Get-Content -Encoding UTF8 -LiteralPath $SpecificationPath) @(Get-Content -Encoding UTF8 -LiteralPath $RegisterPath)
$approved = Get-Content -Raw -Encoding UTF8 -LiteralPath $BaselinePath | ConvertFrom-Json
Assert-IdentitySnapshot $actual $approved
Write-Output 'Specification identity baseline verified; semantic equivalence still requires review.'
