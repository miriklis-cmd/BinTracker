param(
    [string]$OutputDirectory = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root
function Fail([string]$message) { throw "PACKAGE AUDIT FAILED: $message" }

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Audit-BinTracker.ps1')
if ($LASTEXITCODE -ne 0) { Fail 'Source audit failed.' }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Invoke-BinTrackerSecurityChecks.ps1')
if ($LASTEXITCODE -ne 0) { Fail 'Locked dependency, advisory or code-security check failed.' }

[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$version = [string]$props.Project.PropertyGroup.Version
$info = [string]$props.Project.PropertyGroup.InformationalVersion
if ($version -ne $info) { Fail 'Version/InformationalVersion mismatch.' }
$rootName = "BinTracker-v$version"
$zipName = "$rootName.zip"
$out = Join-Path $OutputDirectory $zipName
$checksumOut = "$out.sha256"
if (Test-Path $out) { Remove-Item -Force $out }
if (Test-Path $checksumOut) { Remove-Item -Force $checksumOut }

# Stage with the version-authoritative root name. Exclude build outputs and VCS metadata at every depth.
$stageBase = Join-Path ([System.IO.Path]::GetTempPath()) ("BinTrackerPackage-" + [guid]::NewGuid())
$stageRoot = Join-Path $stageBase $rootName
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
$excludeDirs = @('.git','bin','obj','.vs')
$excludeRootPaths = @('.codex-evidence','BIN-LIN-IMP-03A-HANDOFF.tmp','codex-git.ps1')
Get-ChildItem -LiteralPath $root -Recurse -File -Force | ForEach-Object {
    $relative = $_.FullName.Substring($root.Length).TrimStart([char]'\', [char]'/')
    $segments = $relative -split '[\\/]'
    if (@($segments | Where-Object { $excludeDirs -contains $_ }).Count -gt 0) { return }
    if ($excludeRootPaths -contains $segments[0]) { return }
    $destination = Join-Path $stageRoot $relative
    $destinationDirectory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
}
Compress-Archive -Path $stageRoot -DestinationPath $out -CompressionLevel Optimal

# Reopen ZIP and mechanically verify sole root + embedded version metadata.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($out)
try {
    $roots = @($archive.Entries | ForEach-Object { (($_.FullName -replace '\\','/') -split '/')[0] } | Where-Object { $_ } | Sort-Object -Unique)
    if ($roots.Count -ne 1 -or $roots[0] -ne $rootName) { Fail "ZIP root mismatch: $($roots -join ', ')" }
    $propsEntry = $archive.Entries | Where-Object { ($_.FullName -replace '\\','/') -eq "$rootName/Directory.Build.props" } | Select-Object -First 1
    if (-not $propsEntry) { Fail 'Directory.Build.props missing from ZIP.' }
    $reader = New-Object System.IO.StreamReader($propsEntry.Open())
    try { [xml]$zipProps = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $zv=[string]$zipProps.Project.PropertyGroup.Version; $zi=[string]$zipProps.Project.PropertyGroup.InformationalVersion
    if ($zv -ne $version -or $zi -ne $version) { Fail "Embedded ZIP version mismatch: Version=$zv InformationalVersion=$zi expected=$version" }
} finally { $archive.Dispose(); Remove-Item -Recurse -Force $stageBase -ErrorAction SilentlyContinue }

$hash = (Get-FileHash -LiteralPath $out -Algorithm SHA256).Hash.ToUpperInvariant()
Set-Content -LiteralPath $checksumOut -Value "$hash *$zipName" -Encoding ascii -NoNewline
$manifest = (Get-Content -Raw -LiteralPath $checksumOut).Trim()
if ($manifest -notmatch '^(?<hash>[A-F0-9]{64}) \*(?<file>.+)$' -or
    $matches['file'] -cne $zipName -or $matches['hash'] -cne $hash) {
    Fail 'Generated SHA-256 manifest could not be parsed or did not identify the package exactly.'
}
$verifiedHash = (Get-FileHash -LiteralPath $out -Algorithm SHA256).Hash.ToUpperInvariant()
if ($verifiedHash -cne $matches['hash']) { Fail 'Package SHA-256 verification failed after manifest creation.' }

Write-Host "Package audit passed: $out ; sole root $rootName ; Version/InformationalVersion $version" -ForegroundColor Green
Write-Host "SHA-256 verified: $checksumOut ; $verifiedHash" -ForegroundColor Green
Write-Host 'Integrity checksum emitted. Publisher identity is not authenticated: this package is not cryptographically signed.' -ForegroundColor Yellow
