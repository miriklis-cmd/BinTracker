param(
    [switch]$RunScannerContractTests
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

function Fail([string]$message) {
    Write-Host "SECURITY CHECK FAILED: $message" -ForegroundColor Red
    exit 1
}

function Invoke-DotNet([string]$description, [string[]]$arguments, [bool]$writeOutput = $true) {
    Write-Host $description
    $output = @(& dotnet @arguments 2>&1)
    $exitCode = $LASTEXITCODE
    if ($writeOutput -and $output.Count -gt 0) { $output | ForEach-Object { Write-Host $_ } }
    if ($exitCode -ne 0) {
        Fail "$description Advisory data and package metadata require reachable configured NuGet sources; no scan pass is recorded when they are unavailable."
    }
    return $output
}

$expectedProjects = @(
    [pscustomobject]@{ Path = 'src\BinTracker.Core\BinTracker.Core.csproj'; Framework = 'net8.0' }
    [pscustomobject]@{ Path = 'src\BinTracker.Data\BinTracker.Data.csproj'; Framework = 'net8.0' }
    [pscustomobject]@{ Path = 'src\BinTracker.Services\BinTracker.Services.csproj'; Framework = 'net8.0' }
    [pscustomobject]@{ Path = 'src\BinTracker.WinForms\BinTracker.WinForms.csproj'; Framework = 'net8.0-windows' }
    [pscustomobject]@{ Path = 'tests\BinTracker.UnitTests\BinTracker.UnitTests.csproj'; Framework = 'net8.0' }
    [pscustomobject]@{ Path = 'tests\BinTracker.IntegrationTests\BinTracker.IntegrationTests.csproj'; Framework = 'net8.0' }
    [pscustomobject]@{ Path = 'tests\BinTracker.WinForms.Tests\BinTracker.WinForms.Tests.csproj'; Framework = 'net8.0-windows' }
)

function Convert-ScannerJson([object[]]$output, [string]$description) {
    try {
        $result = ($output -join [Environment]::NewLine) | ConvertFrom-Json
    } catch {
        throw "$description did not return valid machine-readable JSON: $($_.Exception.Message)"
    }

    if ($null -eq $result -or $result.version -ne 1) {
        throw "$description did not return the required JSON output version 1."
    }

    return $result
}

function Get-RequiredArray([object]$instance, [string]$propertyName, [string]$context, [bool]$allowEmpty = $false) {
    if ($null -eq $instance) {
        throw "$context is null."
    }

    $property = $instance.PSObject.Properties[$propertyName]
    if ($null -eq $property -or $property.Value -isnot [System.Array]) {
        throw "$context must contain a '$propertyName' JSON array."
    }

    if (-not $allowEmpty -and $property.Value.Count -eq 0) {
        throw "$context contains an empty '$propertyName' JSON array."
    }

    return @($property.Value)
}

function Get-CanonicalProjectPath([object]$project, [string]$context) {
    if ($null -eq $project -or $project.path -isnot [string] -or [string]::IsNullOrWhiteSpace($project.path)) {
        throw "$context has a missing or malformed project path."
    }

    try {
        $candidatePath = if ([System.IO.Path]::IsPathRooted($project.path)) {
            $project.path
        } else {
            Join-Path $root $project.path
        }
        return [System.IO.Path]::GetFullPath($candidatePath)
    } catch {
        throw "$context has an invalid project path '$($project.path)': $($_.Exception.Message)"
    }
}

function Get-ExpectedProjectMap {
    $map = [System.Collections.Generic.Dictionary[string, string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    foreach ($expectedProject in $expectedProjects) {
        $canonicalPath = [System.IO.Path]::GetFullPath((Join-Path $root $expectedProject.Path))
        if ($map.ContainsKey($canonicalPath)) {
            throw "The security-check configuration duplicates expected project '$canonicalPath'."
        }
        $map.Add($canonicalPath, $expectedProject.Framework)
    }

    return $map
}

function Assert-ProjectIdentities(
    [object]$scan,
    [System.Collections.Generic.Dictionary[string, string]]$expectedProjectMap,
    [string]$description,
    [bool]$requireFrameworks
) {
    $projects = Get-RequiredArray $scan 'projects' $description
    $actualPaths = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    foreach ($project in $projects) {
        $projectPath = Get-CanonicalProjectPath $project "$description project result"
        if (-not $expectedProjectMap.ContainsKey($projectPath)) {
            throw "$description returned unexpected project '$projectPath'."
        }
        if (-not $actualPaths.Add($projectPath)) {
            throw "$description returned duplicate project '$projectPath'."
        }

        $frameworkProperty = $project.PSObject.Properties['frameworks']
        if ($requireFrameworks -or $null -ne $frameworkProperty) {
            $frameworks = Get-RequiredArray $project 'frameworks' "$description project '$projectPath'"
            $actualFrameworks = [System.Collections.Generic.HashSet[string]]::new(
                [System.StringComparer]::OrdinalIgnoreCase)

            foreach ($framework in $frameworks) {
                if ($null -eq $framework -or $framework.framework -isnot [string] -or
                    [string]::IsNullOrWhiteSpace($framework.framework)) {
                    throw "$description project '$projectPath' has a missing or malformed framework identity."
                }
                if (-not $actualFrameworks.Add($framework.framework)) {
                    throw "$description project '$projectPath' returned duplicate framework '$($framework.framework)'."
                }
                if (-not [string]::Equals(
                    $framework.framework,
                    $expectedProjectMap[$projectPath],
                    [System.StringComparison]::OrdinalIgnoreCase)) {
                    throw "$description project '$projectPath' returned unexpected framework '$($framework.framework)'."
                }
            }

            if ($actualFrameworks.Count -ne 1) {
                throw "$description project '$projectPath' did not return exactly its one expected framework."
            }
        }
    }

    if ($actualPaths.Count -ne $expectedProjectMap.Count) {
        $missing = @($expectedProjectMap.Keys | Where-Object { -not $actualPaths.Contains($_) })
        throw "$description did not return all $($expectedProjectMap.Count) expected projects. Missing: $($missing -join ', ')."
    }
}

function Assert-ScannerCoverage([object]$inventory, [object]$vulnerabilityScan) {
    $expectedProjectMap = Get-ExpectedProjectMap
    Assert-ProjectIdentities $inventory $expectedProjectMap 'Package inventory' $true
    Assert-ProjectIdentities $vulnerabilityScan $expectedProjectMap 'Vulnerability scan' $false

    $sources = Get-RequiredArray $vulnerabilityScan 'sources' 'Vulnerability scan'
    $hasOnlineSource = $false
    foreach ($source in $sources) {
        $uri = $null
        if ($source -isnot [string] -or
            -not [System.Uri]::TryCreate($source, [System.UriKind]::Absolute, [ref]$uri)) {
            throw "Vulnerability scan returned malformed advisory source '$source'."
        }
        if ($uri.Scheme -eq [System.Uri]::UriSchemeHttp -or
            $uri.Scheme -eq [System.Uri]::UriSchemeHttps) {
            $hasOnlineSource = $true
        }
    }
    if (-not $hasOnlineSource) {
        throw 'Vulnerability scan did not report an online advisory source.'
    }
}

function New-ScannerContractFixture([int]$projectCount = 7, [bool]$includeFrameworks = $true) {
    $projects = @(
        $expectedProjects | Select-Object -First $projectCount | ForEach-Object {
            $project = [ordered]@{ path = $_.Path }
            if ($includeFrameworks) {
                $project.frameworks = @([ordered]@{ framework = $_.Framework })
            }
            [pscustomobject]$project
        }
    )

    return [pscustomobject]@{
        version = 1
        sources = @('https://api.nuget.org/v3/index.json')
        projects = $projects
    }
}

function Invoke-ScannerContractTests {
    $cases = @(
        [pscustomobject]@{
            Name = 'empty projects'
            ExpectedMessage = "contains an empty 'projects' JSON array"
            Inventory = New-ScannerContractFixture
            Scan = [pscustomobject]@{
                version = 1
                sources = @('https://api.nuget.org/v3/index.json')
                projects = @()
            }
        }
        [pscustomobject]@{
            Name = 'partial projects'
            ExpectedMessage = 'did not return all 7 expected projects'
            Inventory = New-ScannerContractFixture
            Scan = New-ScannerContractFixture -projectCount 6 -includeFrameworks $false
        }
        [pscustomobject]@{
            Name = 'duplicate project'
            ExpectedMessage = 'returned duplicate project'
            Inventory = New-ScannerContractFixture
            Scan = New-ScannerContractFixture -includeFrameworks $false
        }
        [pscustomobject]@{
            Name = 'malformed project'
            ExpectedMessage = 'has a missing or malformed project path'
            Inventory = New-ScannerContractFixture
            Scan = New-ScannerContractFixture -includeFrameworks $false
        }
        [pscustomobject]@{
            Name = 'missing framework'
            ExpectedMessage = "must contain a 'frameworks' JSON array"
            Inventory = New-ScannerContractFixture -includeFrameworks $false
            Scan = New-ScannerContractFixture -includeFrameworks $false
        }
        [pscustomobject]@{
            Name = 'duplicate framework'
            ExpectedMessage = 'returned duplicate framework'
            Inventory = New-ScannerContractFixture
            Scan = New-ScannerContractFixture -includeFrameworks $false
        }
    )

    $cases[2].Scan.projects[1].path = $cases[2].Scan.projects[0].path
    $cases[3].Scan.projects[0].path = $null
    $cases[5].Inventory.projects[0].frameworks = @(
        [pscustomobject]@{ framework = $expectedProjects[0].Framework }
        [pscustomobject]@{ framework = $expectedProjects[0].Framework }
    )

    foreach ($case in $cases) {
        $failedClosed = $false
        try {
            Assert-ScannerCoverage $case.Inventory $case.Scan
        } catch {
            $failedClosed = $true
            if ($_.Exception.Message -notlike "*$($case.ExpectedMessage)*") {
                throw "Scanner contract test '$($case.Name)' failed for the wrong reason: $($_.Exception.Message)"
            }
        }
        if (-not $failedClosed) {
            throw "Scanner contract test '$($case.Name)' did not fail closed."
        }
    }

    $validInventory = New-ScannerContractFixture
    $validZeroVulnerabilityScan = New-ScannerContractFixture -includeFrameworks $false
    Assert-ScannerCoverage $validInventory $validZeroVulnerabilityScan
    Write-Host "Scanner contract tests passed: $($cases.Count) malformed/incomplete cases rejected; valid zero-vulnerability output accepted." -ForegroundColor Green
}

if ($RunScannerContractTests) {
    try {
        Invoke-ScannerContractTests
        exit 0
    } catch {
        Fail $_.Exception.Message
    }
}

$expectedSdk = '10.0.403'
$actualSdk = (& dotnet --version 2>&1 | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $actualSdk -ne $expectedSdk) {
    Fail "Expected pinned .NET SDK $expectedSdk but resolved '$actualSdk'. Install the pinned SDK; do not change global.json merely to bypass this gate."
}

Invoke-DotNet 'Restoring the locked dependency graph...' @(
    'restore', '.\BinTracker.sln', '--locked-mode', '--disable-parallel', '-m:1', '/nr:false'
) | Out-Null

$inventoryOutput = @(Invoke-DotNet 'Inventorying all resolved direct and transitive packages...' @(
    'list', '.\BinTracker.sln', 'package', '--include-transitive',
    '--format', 'json', '--output-version', '1', '--no-restore'
) $false)

$scanOutput = @(Invoke-DotNet 'Scanning all resolved direct and transitive packages for known vulnerabilities...' @(
    'list', '.\BinTracker.sln', 'package', '--vulnerable', '--include-transitive',
    '--format', 'json', '--output-version', '1', '--no-restore'
))

try {
    $inventory = Convert-ScannerJson $inventoryOutput 'Package inventory'
    $scan = Convert-ScannerJson $scanOutput 'Vulnerability scan'
    Assert-ScannerCoverage $inventory $scan
} catch {
    Fail $_.Exception.Message
}

$findings = [System.Collections.Generic.List[object]]::new()
foreach ($project in @($scan.projects)) {
    foreach ($framework in @($project.frameworks)) {
        foreach ($kind in @('topLevelPackages', 'transitivePackages')) {
            foreach ($package in @($framework.$kind)) {
                foreach ($vulnerability in @($package.vulnerabilities)) {
                    if ($null -ne $vulnerability) {
                        $findings.Add([pscustomobject]@{
                            Project = $project.path
                            Framework = $framework.framework
                            Kind = $kind
                            Package = $package.id
                            Version = $package.resolvedVersion
                            Severity = $vulnerability.severity
                            Advisory = $vulnerability.advisoryUrl
                        })
                    }
                }
            }
        }
    }
}

if ($findings.Count -gt 0) {
    $findings | Format-Table -AutoSize | Out-Host
    Fail "$($findings.Count) known-vulnerability finding(s) affect the locked dependency graph. Update or formally resolve the dependencies and regenerate reviewed lock files."
}

Invoke-DotNet 'Building Release with the SDK recommended security-category analyzers enabled as errors...' @(
    'build', '.\BinTracker.sln', '--configuration', 'Release', '--no-restore',
    '-m:1', '/nr:false', '/p:RunAnalyzers=true'
) | Out-Null

Write-Host 'Security checks passed: locked restore; online direct/transitive vulnerability scan; Release security-analyzer build.' -ForegroundColor Green
