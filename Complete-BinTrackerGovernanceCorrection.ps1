param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Verify','Commit')]
    [string]$Phase,
    [ValidatePattern('^[0-9a-f]{64}$')]
    [string]$ExpectedCandidateSha256,
    [switch]$IndependentSourceReviewPassed,
    [switch]$IndependentBatReviewPassed,
    [string]$EvidencePath,
    [ValidatePattern('^[0-9a-f]{64}$')]
    [string]$ExpectedEvidenceSha256,
    [switch]$PrintFingerprint,
    [string]$RepositoryRoot,
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$ExpectedBaseline = '1b0c2006a9207f0964ed481ef95cda016f4e92d5',
    [string]$ExpectedBranch = 'master'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$scriptDirectory = Split-Path -Parent $PSCommandPath
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = $scriptDirectory }
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
Set-Location $root
. (Join-Path $scriptDirectory 'Audit-BinTrackerGovernance.ps1')

$allowedPaths = @(
    'AGENTS.md',
    'Audit-BinTracker.ps1',
    'Audit-BinTrackerGovernance.ps1',
    'Complete-BinTrackerGovernanceCorrection.ps1',
    'KNOWN-ISSUES.md',
    'TECH-DEBT.md',
    'TEST-CHECKLIST.md',
    'Test-AuditBinTrackerGovernance.ps1',
    'docs/CHANGELOG.md',
    'docs/CONTINUATION.md',
    'docs/DevelopmentWorkflow.md',
    'docs/DocumentationAudit.md',
    'docs/RELEASE-NOTES.md',
    'docs/RequirementsAcceptanceRegister.md',
    'docs/Roadmap.md',
    'docs/Testing.md',
    'docs/Versioning.md'
) | Sort-Object
$protectedUntracked = @('.codex-evidence/','BIN-LIN-IMP-03A-HANDOFF.tmp','codex-git.ps1')
$commitSubject = 'Repair alpha.21 audit governance'
$commitBody = 'Records the pushed bookkeeping audit failure truthfully and replaces encoding-dependent, over-broad lineage and unbound-index gates. The bookkeeping commit itself was not BAT-tested.'

function Stop-Guard([string]$Message) { throw "GOVERNANCE $Phase GUARD FAILED: $Message" }

function Invoke-GuardGit([string[]]$Arguments) {
    try { return @(Invoke-GitLines $root $Arguments) }
    catch { Stop-Guard $_.Exception.Message }
}

function Get-FileSha256([string]$Path) {
    $hash = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
        try { return ([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $stream.Dispose() }
    }
    finally { $hash.Dispose() }
}

function Get-CandidateStatusPaths {
    $paths = @()
    foreach ($line in @(Invoke-GuardGit @('status','--porcelain=v1','--untracked-files=all'))) {
        if ($line.Length -lt 4) { continue }
        $path = $line.Substring(3).Replace('\','/')
        $isProtected = $false
        foreach ($protected in $protectedUntracked) {
            if ($path -ceq $protected -or $path.StartsWith($protected, [System.StringComparison]::Ordinal)) {
                $isProtected = $true
                break
            }
        }
        if (-not $isProtected) { $paths += $path }
    }
    return @($paths | Sort-Object -Unique)
}

function Assert-ExpectedRefs {
    $branch = (@(Invoke-GuardGit @('branch','--show-current'))[0]).Trim()
    $head = (@(Invoke-GuardGit @('rev-parse','HEAD'))[0]).Trim()
    $upstreamName = (@(Invoke-GuardGit @('rev-parse','--abbrev-ref','--symbolic-full-name','@{upstream}'))[0]).Trim()
    $upstream = (@(Invoke-GuardGit @('rev-parse','@{upstream}'))[0]).Trim()
    $divergence = ((@(Invoke-GuardGit @('rev-list','--left-right','--count','HEAD...@{upstream}'))[0]) -split '\s+')
    if ($branch -cne $ExpectedBranch -or $upstreamName -cne "origin/$ExpectedBranch" -or
        $head -cne $ExpectedBaseline -or $upstream -cne $ExpectedBaseline -or
        $divergence.Count -ne 2 -or [int]$divergence[0] -ne 0 -or [int]$divergence[1] -ne 0) {
        Stop-Guard 'Expected synchronized master at the reviewed bookkeeping baseline.'
    }
}

function Assert-CandidateState([switch]$RequireEmptyIndex) {
    Assert-ExpectedRefs
    if ($RequireEmptyIndex -and @(Invoke-GuardGit @('diff','--cached','--name-only')).Count -ne 0) {
        Stop-Guard 'Staging must be empty.'
    }
    $actualPaths = @(Get-CandidateStatusPaths)
    if (($actualPaths -join "`n") -cne ($allowedPaths -join "`n")) {
        Stop-Guard "Candidate path set differs from the reviewed allowlist: $($actualPaths -join ', ')."
    }
}

function Get-CandidateIdentity {
    try { return Get-GitCanonicalCandidateManifest $root $allowedPaths }
    catch { Stop-Guard $_.Exception.Message }
}

function Assert-ExpectedCandidate([object]$Identity) {
    if ([string]::IsNullOrWhiteSpace($ExpectedCandidateSha256) -or
        $Identity.Sha256 -cne $ExpectedCandidateSha256) {
        Stop-Guard "Reviewed candidate fingerprint mismatch. Actual: $($Identity.Sha256)"
    }
}

function Invoke-ExternalCommand(
    [string]$Label,
    [string]$FileName,
    [string[]]$Arguments,
    [System.IO.StreamWriter]$LogWriter = $null) {
    if ($null -ne $LogWriter) {
        $LogWriter.WriteLine("=== $Label START ===")
        $LogWriter.Flush()
    }
    $priorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = @(& $FileName @Arguments 2>&1) }
    finally { $ErrorActionPreference = $priorPreference }
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        $text = [string]$line
        Write-Host $text
        if ($null -ne $LogWriter) { $LogWriter.WriteLine($text) }
    }
    if ($null -ne $LogWriter) {
        $LogWriter.WriteLine("=== $Label EXIT $exitCode ===")
        $LogWriter.Flush()
    }
    return [pscustomobject]@{ Label = $Label; ExitCode = $exitCode }
}

function Require-Success([object]$Result) {
    if ($Result.ExitCode -ne 0) { Stop-Guard "$($Result.Label) failed with exit code $($Result.ExitCode)." }
}

function Resolve-EvidenceLocation {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) { Stop-Guard 'EvidencePath is required.' }
    $fullPath = [System.IO.Path]::GetFullPath($EvidencePath)
    $rootPrefix = $root.TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Stop-Guard 'BAT evidence must be written outside the candidate repository.'
    }
    return $fullPath
}

function Read-ApprovedEvidence([string]$Path, [object]$CandidateIdentity) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Stop-Guard 'Approved BAT evidence file is missing.' }
    if ([string]::IsNullOrWhiteSpace($ExpectedEvidenceSha256)) { Stop-Guard 'ExpectedEvidenceSha256 is required.' }
    $actualEvidenceHash = Get-FileSha256 $Path
    if ($actualEvidenceHash -cne $ExpectedEvidenceSha256) {
        Stop-Guard "BAT evidence SHA-256 mismatch. Actual: $actualEvidenceHash"
    }
    try {
        $text = ConvertFrom-GovernedUtf8Bytes ([System.IO.File]::ReadAllBytes($Path)) $Path
        $evidence = $text | ConvertFrom-Json
    }
    catch { Stop-Guard "BAT evidence is invalid: $($_.Exception.Message)" }

    if ($evidence.Schema -cne 'BinTracker.GovernanceBatEvidence/v1' -or
        $evidence.Phase -cne 'Verify' -or
        $evidence.RepositoryRoot -cne $root.Replace('\','/') -or
        $evidence.Baseline -cne $ExpectedBaseline -or
        $evidence.Branch -cne $ExpectedBranch -or
        $evidence.Upstream -cne $ExpectedBaseline -or
        $evidence.CandidateManifestSha256 -cne $CandidateIdentity.Sha256 -or
        [bool]$evidence.GitWritesPerformed -or
        [int]$evidence.FocusedGovernanceExitCode -ne 0 -or
        [int]$evidence.MechanicalAuditExitCode -ne 0 -or
        [int]$evidence.DiffCheckExitCode -ne 0 -or
        [int]$evidence.CanonicalBatExitCode -ne 0) {
        Stop-Guard 'BAT evidence does not authorize this exact candidate and successful gate set.'
    }
    if ((@($evidence.CandidatePaths) -join "`n") -cne ($allowedPaths -join "`n")) {
        Stop-Guard 'BAT evidence candidate path set differs from the approved candidate.'
    }
    $logPath = [string]$evidence.OutputLogPath
    if (-not (Test-Path -LiteralPath $logPath -PathType Leaf) -or
        (Get-FileSha256 $logPath) -cne [string]$evidence.OutputLogSha256) {
        Stop-Guard 'BAT evidence output log is missing or has changed.'
    }
    return $evidence
}

function New-ExactTreeCommit([string]$TreeId, [string]$ParentCommit) {
    if ($TreeId -cnotmatch '^[0-9a-f]{40,64}$') { Stop-Guard 'Verified index tree identity is invalid.' }
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = 'git'
    $start.Arguments = "commit-tree $TreeId -p $ParentCommit"
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    [void]$process.Start()
    $process.StandardInput.WriteLine($commitSubject)
    $process.StandardInput.WriteLine()
    $process.StandardInput.WriteLine($commitBody)
    $process.StandardInput.Close()
    $commit = $process.StandardOutput.ReadToEnd().Trim()
    $errorText = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0 -or $commit -cnotmatch '^[0-9a-f]{40,64}$') {
        Stop-Guard "Unable to create exact-tree commit: $errorText"
    }
    return $commit
}

Assert-CandidateState -RequireEmptyIndex
$candidate = Get-CandidateIdentity
if ($PrintFingerprint) {
    Write-Output $candidate.Sha256
    exit 0
}
Assert-ExpectedCandidate $candidate

if ($Phase -ceq 'Verify') {
    if (-not $IndependentSourceReviewPassed) {
        Stop-Guard 'IndependentSourceReviewPassed must be explicitly supplied before the canonical BAT.'
    }
    $evidenceFile = Resolve-EvidenceLocation
    $logFile = "$evidenceFile.log"
    if ((Test-Path -LiteralPath $evidenceFile) -or (Test-Path -LiteralPath $logFile)) {
        Stop-Guard 'EvidencePath and its log path must be new; existing evidence is never overwritten.'
    }
    $evidenceDirectory = Split-Path -Parent $evidenceFile
    if (-not (Test-Path -LiteralPath $evidenceDirectory -PathType Container)) {
        [void](New-Item -ItemType Directory -Path $evidenceDirectory)
    }

    $startedUtc = [DateTime]::UtcNow.ToString('o')
    $writer = New-Object System.IO.StreamWriter($logFile, $false, (Get-StrictUtf8Encoding))
    try {
        $focused = Invoke-ExternalCommand 'FOCUSED GOVERNANCE TESTS' 'powershell.exe' @(
            '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'Test-AuditBinTrackerGovernance.ps1'),
            '-RepositoryRoot',$root,'-Quiet') $writer
        Require-Success $focused
        $audit = Invoke-ExternalCommand 'MECHANICAL AUDIT' 'powershell.exe' @(
            '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'Audit-BinTracker.ps1')) $writer
        Require-Success $audit
        $diffCheck = Invoke-ExternalCommand 'WORKTREE DIFF CHECK' 'git' @('-C',$root,'diff','--check') $writer
        Require-Success $diffCheck
        $bat = Invoke-ExternalCommand 'CANONICAL BAT' 'cmd.exe' @('/d','/c','call',(Join-Path $root 'Build-BinTracker.bat')) $writer
        Require-Success $bat
    }
    finally { $writer.Dispose() }

    Assert-CandidateState -RequireEmptyIndex
    $afterBat = Get-CandidateIdentity
    if ($afterBat.Sha256 -cne $candidate.Sha256) { Stop-Guard 'Candidate changed during canonical BAT.' }
    $logHash = Get-FileSha256 $logFile
    $evidence = [ordered]@{
        Schema = 'BinTracker.GovernanceBatEvidence/v1'
        Phase = 'Verify'
        StartedUtc = $startedUtc
        CompletedUtc = [DateTime]::UtcNow.ToString('o')
        RepositoryRoot = $root.Replace('\','/')
        Branch = $ExpectedBranch
        Baseline = $ExpectedBaseline
        Upstream = $ExpectedBaseline
        CandidateManifestSha256 = $candidate.Sha256
        CandidatePaths = @($allowedPaths)
        FocusedGovernanceExitCode = $focused.ExitCode
        MechanicalAuditExitCode = $audit.ExitCode
        DiffCheckExitCode = $diffCheck.ExitCode
        CanonicalBatExitCode = $bat.ExitCode
        OutputLogPath = $logFile
        OutputLogSha256 = $logHash
        GitWritesPerformed = $false
    }
    [System.IO.File]::WriteAllText($evidenceFile, ($evidence | ConvertTo-Json -Depth 5), (Get-StrictUtf8Encoding))
    $evidenceHash = Get-FileSha256 $evidenceFile
    Write-Host 'GOVERNANCE VERIFY PHASE PASSED WITHOUT GIT WRITES.' -ForegroundColor Green
    Write-Host "BAT EVIDENCE: $evidenceFile"
    Write-Host "BAT EVIDENCE SHA-256: $evidenceHash"
    exit 0
}

if (-not $IndependentBatReviewPassed) {
    Stop-Guard 'IndependentBatReviewPassed must be explicitly supplied before any staging or Git write.'
}
$evidenceFile = Resolve-EvidenceLocation
[void](Read-ApprovedEvidence $evidenceFile $candidate)

$focused = Invoke-ExternalCommand 'FOCUSED GOVERNANCE TESTS' 'powershell.exe' @(
    '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'Test-AuditBinTrackerGovernance.ps1'),
    '-RepositoryRoot',$root,'-Quiet')
Require-Success $focused
$audit = Invoke-ExternalCommand 'MECHANICAL AUDIT' 'powershell.exe' @(
    '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'Audit-BinTracker.ps1'))
Require-Success $audit
$diffCheck = Invoke-ExternalCommand 'WORKTREE DIFF CHECK' 'git' @('-C',$root,'diff','--check')
Require-Success $diffCheck

Assert-CandidateState -RequireEmptyIndex
$preStage = Get-CandidateIdentity
if ($preStage.Sha256 -cne $candidate.Sha256) { Stop-Guard 'Candidate changed after BAT evidence approval.' }

[void](Invoke-GuardGit (@('add','--') + $allowedPaths))
try { [void](Assert-GitIndexMatchesApprovedCandidate $root $allowedPaths $candidate.Sha256) }
catch { Stop-Guard $_.Exception.Message }
if (@(Invoke-GuardGit @('diff','--name-only')).Count -ne 0) { Stop-Guard 'Unstaged tracked changes remain after staging.' }
$stagedCheck = Invoke-ExternalCommand 'STAGED DIFF CHECK' 'git' @('-C',$root,'diff','--cached','--check')
Require-Success $stagedCheck
$stagedAudit = Invoke-ExternalCommand 'STAGED CANDIDATE AUDIT' 'powershell.exe' @(
    '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'Audit-BinTracker.ps1'))
Require-Success $stagedAudit

Assert-ExpectedRefs
$finalCandidate = Get-CandidateIdentity
if ($finalCandidate.Sha256 -cne $candidate.Sha256) { Stop-Guard 'Worktree candidate changed after staging.' }
try { [void](Assert-GitIndexMatchesApprovedCandidate $root $allowedPaths $candidate.Sha256) }
catch { Stop-Guard $_.Exception.Message }
$verifiedTree = (@(Invoke-GuardGit @('write-tree'))[0]).Trim()

$commit = New-ExactTreeCommit $verifiedTree $ExpectedBaseline
Assert-ExpectedRefs
$treeBeforeRefUpdate = (@(Invoke-GuardGit @('write-tree'))[0]).Trim()
if ($treeBeforeRefUpdate -cne $verifiedTree) { Stop-Guard 'Index changed after exact tree verification.' }
try { [void](Assert-GitIndexMatchesApprovedCandidate $root $allowedPaths $candidate.Sha256) }
catch { Stop-Guard $_.Exception.Message }
[void](Invoke-GuardGit @('update-ref',"refs/heads/$ExpectedBranch",$commit,$ExpectedBaseline))

$committedTree = (@(Invoke-GuardGit @('rev-parse',"$commit^{tree}"))[0]).Trim()
if ($committedTree -cne $verifiedTree) { Stop-Guard 'Created commit does not use the verified index tree.' }
[void](Invoke-GuardGit @('push','origin',$ExpectedBranch))

$finalHead = (@(Invoke-GuardGit @('rev-parse','HEAD'))[0]).Trim()
$finalUpstream = (@(Invoke-GuardGit @('rev-parse',"origin/$ExpectedBranch"))[0]).Trim()
$finalDivergence = ((@(Invoke-GuardGit @('rev-list','--left-right','--count',"HEAD...origin/$ExpectedBranch"))[0]) -split '\s+')
if ($finalHead -cne $commit -or $finalHead -cne $finalUpstream -or $finalDivergence.Count -ne 2 -or
    [int]$finalDivergence[0] -ne 0 -or [int]$finalDivergence[1] -ne 0) {
    Stop-Guard 'Push/ref verification failed.'
}
$remaining = @(Get-CandidateStatusPaths)
if ($remaining.Count -ne 0) { Stop-Guard "Tracked/candidate files remain changed after push: $($remaining -join ', ')." }
Write-Host "GOVERNANCE CORRECTION COMMITTED AND PUSHED: $finalHead" -ForegroundColor Green
