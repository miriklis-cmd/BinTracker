param(
    [string]$RepositoryRoot = (Split-Path -Parent $MyInvocation.MyCommand.Path),
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'Audit-BinTrackerGovernance.ps1')

$script:Passed = 0
function Assert-Pass([string]$Name, [scriptblock]$Action) {
    & $Action
    $script:Passed++
    if (-not $Quiet) { Write-Host "PASS $Name" -ForegroundColor Green }
}

function Assert-Rejected([string]$Name, [scriptblock]$Action) {
    $rejected = $false
    try { & $Action } catch { $rejected = $true }
    if (-not $rejected) { throw "Expected rejection: $Name" }
    $script:Passed++
    if (-not $Quiet) { Write-Host "PASS $Name" -ForegroundColor Green }
}

function Write-Utf8([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText($Path, $Text, (Get-StrictUtf8Encoding))
}

function Run-Git([string]$Root, [string[]]$Arguments) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = @(& git -C $Root @Arguments 2>&1) }
    finally { $ErrorActionPreference = $previousPreference }
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed: $($output -join "`n")" }
    return @($output)
}

$unicodeEvidence = ([char]0x2014) + ' caf' + ([char]0x00e9) + ' ' + ([char]0x03a9)
$unicodeBase = "# Active`n`n## Active base`nbase`n`n## Historical checkpoint $unicodeEvidence`nunchanged`n"
$unicodeCurrent = "# Active`n`n## Active current`ncurrent`n`n## Historical checkpoint $unicodeEvidence`nunchanged`n"
Assert-Pass 'unchanged-historical-unicode' {
    Assert-ContinuationHistoricalPreserved $unicodeBase $unicodeCurrent
}
Assert-Pass 'crlf-lf-normalization-only' {
    Assert-ContinuationHistoricalPreserved ($unicodeBase -replace "`n", "`r`n") $unicodeCurrent
}
Assert-Rejected 'historical-modification-rejected' {
    Assert-ContinuationHistoricalPreserved $unicodeBase ($unicodeCurrent -replace 'unchanged', 'changed')
}
Assert-Rejected 'missing-historical-boundary-rejected' {
    Assert-ContinuationHistoricalPreserved $unicodeBase ($unicodeCurrent -replace '## Historical checkpoint', '## Prior checkpoint')
}
Assert-Rejected 'changed-historical-boundary-rejected' {
    Assert-ContinuationHistoricalPreserved $unicodeBase ($unicodeCurrent -replace '## Historical checkpoint', '## Superseded checkpoint')
}
$failureEvidence = '1024/1024; AUDIT FAILED: Post-acceptance bookkeeping modified historical CONTINUATION content. That progression is a governance failure, not a PASS. P15 also remains non-compliant because its pasted user input contained assistant-return boundaries.'
Assert-Pass 'failed-p15-evidence-retained' {
    Assert-Alpha21FailureEvidence $failureEvidence
}
Assert-Rejected 'failed-p15-evidence-removal-rejected' {
    Assert-Alpha21FailureEvidence ($failureEvidence -replace 'P15 also remains non-compliant', 'P15 passed')
}

$acceptedApplication = '94c43d7138ba0e52566575ffb303518fd5530c86'
$bookkeepingCommit = '1b0c2006a9207f0964ed481ef95cda016f4e92d5'
Assert-Pass 'actual-alpha21-bookkeeping-scenario' {
    Assert-PostAcceptanceBookkeepingTransition $RepositoryRoot $acceptedApplication $bookkeepingCommit
}
Assert-Rejected 'unapproved-ancestry-rejected' {
    Assert-PostAcceptanceBookkeepingTransition $RepositoryRoot $bookkeepingCommit $acceptedApplication
}

$tempRoot = [System.IO.Path]::GetTempPath().TrimEnd('\')
$fixture = Join-Path $tempRoot ("BinTrackerGovernance-{0}" -f [guid]::NewGuid().ToString('N'))
try {
    [void](New-Item -ItemType Directory -Path $fixture)
    [void](Run-Git $fixture @('init','-b','master'))
    [void](Run-Git $fixture @('config','core.autocrlf','false'))
    [void](Run-Git $fixture @('config','user.name','BinTracker Audit Test'))
    [void](Run-Git $fixture @('config','user.email','audit@example.invalid'))
    [void](New-Item -ItemType Directory -Path (Join-Path $fixture 'docs'))

    $baseContinuation = "# Active BinTracker Continuation Checkpoint`n`n## Active base`nBase state.`n`n## Historical Unicode $unicodeEvidence`nImmutable evidence.`n"
    $baseAudit = "# Documentation Audit Record`n`n## Existing entry`n`n- Existing evidence.`n"
    Write-Utf8 (Join-Path $fixture 'docs/CONTINUATION.md') $baseContinuation
    Write-Utf8 (Join-Path $fixture 'docs/DocumentationAudit.md') $baseAudit
    Write-Utf8 (Join-Path $fixture 'candidate.txt') "accepted application`n"
    [void](Run-Git $fixture @('add','--','docs/CONTINUATION.md','docs/DocumentationAudit.md','candidate.txt'))
    [void](Run-Git $fixture @('commit','-m','accepted application'))
    $accepted = (@(Run-Git $fixture @('rev-parse','HEAD'))[0]).Trim()

    $bookContinuation = "# Active BinTracker Continuation Checkpoint`n`n## Active closure`nMode: POST-ACCEPTANCE EVIDENCE BOOKKEEPING`nAccepted application ${accepted}.`nThe bookkeeping commit itself was not BAT-tested.`n`n## Historical Unicode $unicodeEvidence`nImmutable evidence.`n"
    $bookEntry = "## Closure evidence`n`n- Mode: POST-ACCEPTANCE EVIDENCE BOOKKEEPING`n- Accepted $accepted after 1024/1024 tests.`n- The bookkeeping commit itself was not BAT-tested.`n`n"
    Write-Utf8 (Join-Path $fixture 'docs/CONTINUATION.md') $bookContinuation
    Write-Utf8 (Join-Path $fixture 'docs/DocumentationAudit.md') ("# Documentation Audit Record`n`n$bookEntry## Existing entry`n`n- Existing evidence.`n")
    [void](Run-Git $fixture @('add','--','docs/CONTINUATION.md','docs/DocumentationAudit.md'))
    [void](Run-Git $fixture @('commit','-m','bookkeeping'))
    $book = (@(Run-Git $fixture @('rev-parse','HEAD'))[0]).Trim()

    Assert-Pass 'legitimate-accepted-app-plus-bookkeeping-transition' {
        Assert-PostAcceptanceBookkeepingTransition $fixture $accepted $book
    }

    [void](Run-Git $fixture @('switch','-c','bad-scope',$accepted))
    Write-Utf8 (Join-Path $fixture 'docs/CONTINUATION.md') $bookContinuation
    Write-Utf8 (Join-Path $fixture 'docs/DocumentationAudit.md') ("# Documentation Audit Record`n`n$bookEntry## Existing entry`n`n- Existing evidence.`n")
    Write-Utf8 (Join-Path $fixture 'candidate.txt') "changed candidate definition`n"
    [void](Run-Git $fixture @('add','--','docs/CONTINUATION.md','docs/DocumentationAudit.md','candidate.txt'))
    [void](Run-Git $fixture @('commit','-m','invalid bookkeeping scope'))
    $badScope = (@(Run-Git $fixture @('rev-parse','HEAD'))[0]).Trim()
    Assert-Rejected 'candidate-defining-file-in-bookkeeping-rejected' {
        Assert-PostAcceptanceBookkeepingTransition $fixture $accepted $badScope
    }

    [void](Run-Git $fixture @('switch','-c','missing-disclaimer',$accepted))
    Write-Utf8 (Join-Path $fixture 'docs/CONTINUATION.md') ($bookContinuation -replace 'The bookkeeping commit itself was not BAT-tested\.', 'Disclaimer omitted.')
    Write-Utf8 (Join-Path $fixture 'docs/DocumentationAudit.md') (("# Documentation Audit Record`n`n$bookEntry## Existing entry`n`n- Existing evidence.`n") -replace 'The bookkeeping commit itself was not BAT-tested\.', 'Disclaimer omitted.')
    [void](Run-Git $fixture @('add','--','docs/CONTINUATION.md','docs/DocumentationAudit.md'))
    [void](Run-Git $fixture @('commit','-m','missing disclaimer'))
    $missingDisclaimer = (@(Run-Git $fixture @('rev-parse','HEAD'))[0]).Trim()
    Assert-Rejected 'missing-exact-bat-disclaimer-rejected' {
        Assert-PostAcceptanceBookkeepingTransition $fixture $accepted $missingDisclaimer
    }

    [void](Run-Git $fixture @('switch','master'))
    [void](Run-Git $fixture @('remote','add','origin',$fixture))
    [void](Run-Git $fixture @('update-ref','refs/remotes/origin/master',$book))
    [void](Run-Git $fixture @('branch','--set-upstream-to','origin/master','master'))
    Write-Utf8 (Join-Path $fixture 'Audit-BinTracker.ps1') "corrected audit`n"
    [void](Run-Git $fixture @('add','--','Audit-BinTracker.ps1'))
    [void](Run-Git $fixture @('commit','-m','governance correction'))
    $correction = (@(Run-Git $fixture @('rev-parse','HEAD'))[0]).Trim()
    [void](Run-Git $fixture @('update-ref','refs/remotes/origin/master',$correction))
    Assert-Pass 'descendant-head-without-self-referential-sha' {
        [void](Assert-DevelopmentLineage $fixture $book)
        $active = [System.IO.File]::ReadAllText((Join-Path $fixture 'docs/CONTINUATION.md'), (Get-StrictUtf8Encoding))
        $head = (@(Run-Git $fixture @('rev-parse','HEAD'))[0]).Trim()
        if ($active.Contains($head)) { throw 'Fixture unexpectedly embeds its future correction SHA.' }
    }

    [void](Run-Git $fixture @('switch','-c','topic'))
    [void](Run-Git $fixture @('branch','--set-upstream-to','origin/master','topic'))
    Assert-Pass 'optional-development-topic-branch-accepted' {
        [void](Assert-DevelopmentLineage $fixture $book)
    }

    [void](Run-Git $fixture @('switch','master'))
    [void](Run-Git $fixture @('switch','-c','reviewed-work'))
    Write-Utf8 (Join-Path $fixture 'reviewed.txt') "reviewed reintegration`n"
    [void](Run-Git $fixture @('add','--','reviewed.txt'))
    [void](Run-Git $fixture @('commit','-m','reviewed work'))
    [void](Run-Git $fixture @('switch','master'))
    [void](Run-Git $fixture @('merge','--no-ff','reviewed-work','-m','reviewed reintegration'))
    $reviewedMerge = (@(Run-Git $fixture @('rev-parse','HEAD'))[0]).Trim()
    Assert-Pass 'legitimate-reviewed-reintegration-accepted' {
        [void](Assert-DevelopmentLineage $fixture $book @($reviewedMerge))
    }
    Assert-Rejected 'unapproved-merge-rejected' {
        [void](Assert-DevelopmentLineage $fixture $book)
    }
    Assert-Rejected 'forged-reviewed-merge-record-rejected' {
        [void](Assert-DevelopmentLineage $fixture $book @($correction))
    }
    Assert-Rejected 'forged-bookkeeping-ancestry-rejected' {
        [void](Assert-DevelopmentLineage $fixture $badScope @($reviewedMerge))
    }

    Write-Utf8 (Join-Path $fixture 'local-only.txt') "not pushed`n"
    [void](Run-Git $fixture @('add','--','local-only.txt'))
    [void](Run-Git $fixture @('commit','-m','local divergence'))
    Assert-Pass 'ordinary-divergence-not-a-global-audit-ban' {
        [void](Assert-DevelopmentLineage $fixture $book @($reviewedMerge))
    }

    Write-Utf8 (Join-Path $fixture 'candidate.txt') "approved staged bytes`n"
    $approvedManifest = Get-GitCanonicalCandidateManifest $fixture @('candidate.txt')
    [void](Run-Git $fixture @('add','--','candidate.txt'))
    Assert-Pass 'approved-staged-bytes-accepted' {
        [void](Assert-GitIndexMatchesApprovedCandidate $fixture @('candidate.txt') $approvedManifest.Sha256)
    }
    Write-Utf8 (Join-Path $fixture 'candidate.txt') "modified staged bytes`n"
    [void](Run-Git $fixture @('add','--','candidate.txt'))
    Assert-Rejected 'modified-staged-bytes-rejected' {
        [void](Assert-GitIndexMatchesApprovedCandidate $fixture @('candidate.txt') $approvedManifest.Sha256)
    }
}
finally {
    foreach ($path in @($fixture)) {
        if (Test-Path -LiteralPath $path) {
            $resolved = (Resolve-Path -LiteralPath $path).Path
            if (-not $resolved.StartsWith($tempRoot + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
                ([System.IO.Path]::GetFileName($resolved) -notlike 'BinTrackerGovernance-*')) {
                throw "Refusing to remove unexpected test path: $resolved"
            }
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
}

$completionScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'Complete-BinTrackerGovernanceCorrection.ps1'
$operatorPaths = @(
    'AGENTS.md','Audit-BinTracker.ps1','Audit-BinTrackerGovernance.ps1',
    'Complete-BinTrackerGovernanceCorrection.ps1','KNOWN-ISSUES.md','TECH-DEBT.md',
    'TEST-CHECKLIST.md','Test-AuditBinTrackerGovernance.ps1','docs/CHANGELOG.md',
    'docs/CONTINUATION.md','docs/DevelopmentWorkflow.md','docs/DocumentationAudit.md',
    'docs/RELEASE-NOTES.md','docs/RequirementsAcceptanceRegister.md','docs/Roadmap.md',
    'docs/Testing.md','docs/Versioning.md'
) | Sort-Object
$operatorArtifacts = @()

function New-OperatorFixture([string]$FailurePoint = '') {
    $id = [guid]::NewGuid().ToString('N')
    $repository = Join-Path $tempRoot "BinTrackerGovernanceOperator-$id"
    $remote = Join-Path $tempRoot "BinTrackerGovernanceRemote-$id.git"
    $marker = Join-Path $tempRoot "BinTrackerGovernanceBat-$id.marker"
    $script:operatorArtifacts += @($repository,$remote,$marker)
    [void](New-Item -ItemType Directory -Path $repository)
    [void](Run-Git $repository @('init','-b','master'))
    [void](Run-Git $repository @('config','core.autocrlf','false'))
    [void](Run-Git $repository @('config','user.name','BinTracker Guard Test'))
    [void](Run-Git $repository @('config','user.email','guard@example.invalid'))
    [void](Run-Git $tempRoot @('init','--bare',$remote))
    [void](Run-Git $repository @('remote','add','origin',$remote))

    foreach ($path in $operatorPaths + @('Build-BinTracker.bat')) {
        $fullPath = Join-Path $repository $path
        $directory = Split-Path -Parent $fullPath
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
            [void](New-Item -ItemType Directory -Path $directory -Force)
        }
        Write-Utf8 $fullPath "baseline $path`n"
    }
    $batExit = if ($FailurePoint -ceq 'Bat') { 19 } else { 0 }
    $batContent = "@echo off`r`n" + 'echo invoked>"' + $marker + '"' + "`r`nexit /b $batExit`r`n"
    Write-Utf8 (Join-Path $repository 'Build-BinTracker.bat') $batContent
    [void](Run-Git $repository (@('add','--') + $operatorPaths + @('Build-BinTracker.bat')))
    [void](Run-Git $repository @('commit','-m','fixture baseline'))
    $baseline = (@(Run-Git $repository @('rev-parse','HEAD'))[0]).Trim()
    $alternateObjects = (Join-Path $repository '.git/objects').Replace('\','/')
    Write-Utf8 (Join-Path $remote 'objects/info/alternates') "$alternateObjects`n"
    [void](Run-Git $remote @('update-ref','refs/heads/master',$baseline))
    [void](Run-Git $repository @('update-ref','refs/remotes/origin/master',$baseline))
    [void](Run-Git $repository @('branch','--set-upstream-to','origin/master','master'))

    foreach ($path in $operatorPaths) {
        Write-Utf8 (Join-Path $repository $path) "candidate $path`n"
    }
    $focusedExit = if ($FailurePoint -ceq 'Focused') { 17 } else { 0 }
    $auditExit = if ($FailurePoint -ceq 'Audit') { 18 } else { 0 }
    Write-Utf8 (Join-Path $repository 'Test-AuditBinTrackerGovernance.ps1') "param([string]`$RepositoryRoot,[switch]`$Quiet)`nexit $focusedExit`n"
    Write-Utf8 (Join-Path $repository 'Audit-BinTracker.ps1') "exit $auditExit`n"
    if ($FailurePoint -ceq 'Diff') {
        Write-Utf8 (Join-Path $repository 'KNOWN-ISSUES.md') "candidate with trailing whitespace   `n"
    }

    return [pscustomobject]@{
        Root = $repository
        Remote = $remote
        Baseline = $baseline
        BatMarker = $marker
        Candidate = Get-GitCanonicalCandidateManifest $repository $operatorPaths
    }
}

function Invoke-Completion([object]$Fixture, [string[]]$Arguments) {
    $priorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $processArguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$completionScript) +
        $Arguments + @('-RepositoryRoot',$Fixture.Root,'-ExpectedBaseline',$Fixture.Baseline,'-ExpectedBranch','master')
    try {
        $output = @(& powershell.exe @processArguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $priorPreference }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = @($output | ForEach-Object { [string]$_ }) }
}

try {
    $phaseFixture = New-OperatorFixture
    $evidencePath = Join-Path $tempRoot ("BinTrackerGovernanceEvidence-{0}.json" -f [guid]::NewGuid().ToString('N'))
    $operatorArtifacts += @($evidencePath,"$evidencePath.log")
    $verify = Invoke-Completion $phaseFixture @(
        '-Phase','Verify','-ExpectedCandidateSha256',$phaseFixture.Candidate.Sha256,
        '-IndependentSourceReviewPassed','-EvidencePath',$evidencePath)
    Assert-Pass 'bat-phase-exits-without-stage-commit-or-push' {
        if ($verify.ExitCode -ne 0) { throw "Verify phase failed: $($verify.Output -join "`n")" }
        $head = (@(Run-Git $phaseFixture.Root @('rev-parse','HEAD'))[0]).Trim()
        $upstream = (@(Run-Git $phaseFixture.Root @('rev-parse','origin/master'))[0]).Trim()
        if ($head -cne $phaseFixture.Baseline -or $upstream -cne $phaseFixture.Baseline) { throw 'Verify phase changed a ref.' }
        if (@(Run-Git $phaseFixture.Root @('diff','--cached','--name-only')).Count -ne 0) { throw 'Verify phase staged content.' }
        if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf) -or
            -not (Test-Path -LiteralPath "$evidencePath.log" -PathType Leaf)) { throw 'Verify phase did not persist review evidence.' }
    }

    $evidenceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $evidencePath).Hash.ToLowerInvariant()
    $missingReview = Invoke-Completion $phaseFixture @(
        '-Phase','Commit','-ExpectedCandidateSha256',$phaseFixture.Candidate.Sha256,
        '-EvidencePath',$evidencePath,'-ExpectedEvidenceSha256',$evidenceHash)
    Assert-Pass 'missing-independent-bat-review-blocks-git' {
        if ($missingReview.ExitCode -eq 0) { throw 'Commit phase accepted missing independent BAT review.' }
        if (@(Run-Git $phaseFixture.Root @('diff','--cached','--name-only')).Count -ne 0) { throw 'Missing-review rejection staged content.' }
        if ((@(Run-Git $phaseFixture.Root @('rev-parse','HEAD'))[0]).Trim() -cne $phaseFixture.Baseline) { throw 'Missing-review rejection committed.' }
    }

    $logBytes = [System.IO.File]::ReadAllBytes("$evidencePath.log")
    [System.IO.File]::AppendAllText("$evidencePath.log", "tampered`n", (Get-StrictUtf8Encoding))
    $staleEvidence = Invoke-Completion $phaseFixture @(
        '-Phase','Commit','-ExpectedCandidateSha256',$phaseFixture.Candidate.Sha256,
        '-IndependentBatReviewPassed','-EvidencePath',$evidencePath,
        '-ExpectedEvidenceSha256',$evidenceHash)
    Assert-Pass 'stale-test-evidence-blocks-commit' {
        if ($staleEvidence.ExitCode -eq 0) { throw 'Commit phase accepted altered BAT output evidence.' }
        if (@(Run-Git $phaseFixture.Root @('diff','--cached','--name-only')).Count -ne 0) { throw 'Stale-evidence rejection staged content.' }
        if ((@(Run-Git $phaseFixture.Root @('rev-parse','HEAD'))[0]).Trim() -cne $phaseFixture.Baseline) { throw 'Stale-evidence rejection committed.' }
    }
    [System.IO.File]::WriteAllBytes("$evidencePath.log", $logBytes)

    $knownIssuesPath = Join-Path $phaseFixture.Root 'KNOWN-ISSUES.md'
    $knownIssuesBytes = [System.IO.File]::ReadAllBytes($knownIssuesPath)
    [System.IO.File]::AppendAllText($knownIssuesPath, "stale candidate`n", (Get-StrictUtf8Encoding))
    $staleCandidate = Invoke-Completion $phaseFixture @(
        '-Phase','Commit','-ExpectedCandidateSha256',$phaseFixture.Candidate.Sha256,
        '-IndependentBatReviewPassed','-EvidencePath',$evidencePath,
        '-ExpectedEvidenceSha256',$evidenceHash)
    Assert-Pass 'stale-candidate-blocks-commit' {
        if ($staleCandidate.ExitCode -eq 0) { throw 'Commit phase accepted a changed candidate.' }
        if (@(Run-Git $phaseFixture.Root @('diff','--cached','--name-only')).Count -ne 0) { throw 'Stale-candidate rejection staged content.' }
        if ((@(Run-Git $phaseFixture.Root @('rev-parse','HEAD'))[0]).Trim() -cne $phaseFixture.Baseline) { throw 'Stale-candidate rejection committed.' }
    }
    [System.IO.File]::WriteAllBytes($knownIssuesPath, $knownIssuesBytes)

    $commit = Invoke-Completion $phaseFixture @(
        '-Phase','Commit','-ExpectedCandidateSha256',$phaseFixture.Candidate.Sha256,
        '-IndependentBatReviewPassed','-EvidencePath',$evidencePath,
        '-ExpectedEvidenceSha256',$evidenceHash)
    Assert-Pass 'approved-evidence-and-index-commit-exact-tree' {
        if ($commit.ExitCode -ne 0) { throw "Commit phase failed: $($commit.Output -join "`n")" }
        $head = (@(Run-Git $phaseFixture.Root @('rev-parse','HEAD'))[0]).Trim()
        $upstream = (@(Run-Git $phaseFixture.Root @('rev-parse','origin/master'))[0]).Trim()
        if ($head -ceq $phaseFixture.Baseline -or $head -cne $upstream) { throw 'Commit phase did not publish exactly one synchronized successor.' }
        if (@(Run-Git $phaseFixture.Root @('status','--porcelain=v1')).Count -ne 0) { throw 'Commit phase left fixture changes.' }
    }

    foreach ($failurePoint in @('Focused','Audit','Diff','Bat')) {
        $failureFixture = New-OperatorFixture $failurePoint
        $failureEvidence = Join-Path $tempRoot ("BinTrackerGovernanceFailure-{0}-{1}.json" -f $failurePoint,[guid]::NewGuid().ToString('N'))
        $operatorArtifacts += @($failureEvidence,"$failureEvidence.log")
        $failedVerify = Invoke-Completion $failureFixture @(
            '-Phase','Verify','-ExpectedCandidateSha256',$failureFixture.Candidate.Sha256,
            '-IndependentSourceReviewPassed','-EvidencePath',$failureEvidence)
        Assert-Pass "failed-$($failurePoint.ToLowerInvariant())-prerequisite-blocks-git" {
            if ($failedVerify.ExitCode -eq 0) { throw "$failurePoint failure unexpectedly passed." }
            if ((@(Run-Git $failureFixture.Root @('rev-parse','HEAD'))[0]).Trim() -cne $failureFixture.Baseline) { throw "$failurePoint failure committed." }
            if ((@(Run-Git $failureFixture.Root @('rev-parse','origin/master'))[0]).Trim() -cne $failureFixture.Baseline) { throw "$failurePoint failure pushed." }
            if (@(Run-Git $failureFixture.Root @('diff','--cached','--name-only')).Count -ne 0) { throw "$failurePoint failure staged content." }
            if (Test-Path -LiteralPath $failureEvidence) { throw "$failurePoint failure created successful evidence." }
            if ($failurePoint -cne 'Bat' -and (Test-Path -LiteralPath $failureFixture.BatMarker)) { throw "$failurePoint failure still ran BAT." }
        }
    }
}
finally {
    foreach ($path in @($operatorArtifacts | Sort-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $resolved = (Resolve-Path -LiteralPath $path).Path
        if (-not $resolved.StartsWith($tempRoot + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
            [System.IO.Path]::GetFileName($resolved) -notlike 'BinTrackerGovernance*') {
            throw "Refusing to remove unexpected operator-test path: $resolved"
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

if (-not $Quiet) {
    Write-Host "BinTracker audit-governance regression tests passed: $script:Passed/$script:Passed." -ForegroundColor Green
}
