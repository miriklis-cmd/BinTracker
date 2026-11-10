Set-StrictMode -Version Latest

function Get-StrictUtf8Encoding {
    return New-Object System.Text.UTF8Encoding($false, $true)
}

function ConvertFrom-GovernedUtf8Bytes([byte[]]$Bytes, [string]$Description) {
    try {
        return (Get-StrictUtf8Encoding).GetString($Bytes)
    }
    catch {
        throw "$Description is not valid UTF-8: $($_.Exception.Message)"
    }
}

function ConvertTo-GovernedText([string]$Text) {
    # Governed repository text is LF-normalized. CRLF is accepted at the
    # worktree boundary so core.autocrlf cannot manufacture a semantic change.
    # No other whitespace or Unicode normalization is permitted.
    return $Text.Replace("`r`n", "`n")
}

function Get-GovernedFileText([string]$Path) {
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $bytes = [System.IO.File]::ReadAllBytes($resolved)
    return ConvertTo-GovernedText (ConvertFrom-GovernedUtf8Bytes $bytes $resolved)
}

function Get-GitBlobBytes([string]$RepositoryRoot, [string]$Commit, [string]$Path) {
    if ($Commit -cnotmatch '^[0-9a-f]{40}$') { throw "Invalid full commit SHA: $Commit" }
    if ($Path -cnotmatch '^[A-Za-z0-9._/-]+$') { throw "Unsafe Git blob path: $Path" }

    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = 'git'
    $start.Arguments = "cat-file blob $Commit`:$Path"
    $start.WorkingDirectory = $RepositoryRoot
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    [void]$process.Start()
    $buffer = New-Object System.IO.MemoryStream
    $process.StandardOutput.BaseStream.CopyTo($buffer)
    $errorText = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Unable to read Git blob $Commit`:$Path. $errorText"
    }
    return $buffer.ToArray()
}

function Get-GitBlobText([string]$RepositoryRoot, [string]$Commit, [string]$Path) {
    $bytes = Get-GitBlobBytes $RepositoryRoot $Commit $Path
    return ConvertTo-GovernedText (ConvertFrom-GovernedUtf8Bytes $bytes "$Commit`:$Path")
}

function Split-ContinuationText([string]$Text, [string]$Description) {
    $boundary = [regex]::new('(?m)(?=^## (?:Superseded|Historical) )')
    $parts = $boundary.Split((ConvertTo-GovernedText $Text), 2)
    if ($parts.Count -ne 2) {
        throw "$Description must contain one operative prefix followed by a Superseded/Historical section boundary."
    }
    return [pscustomobject]@{
        Operative = $parts[0]
        Historical = $parts[1]
    }
}

function Assert-ContinuationHistoricalPreserved(
    [string]$BaseText,
    [string]$CurrentText,
    [string]$BaseDescription = 'base continuation',
    [string]$CurrentDescription = 'current continuation') {
    $base = Split-ContinuationText $BaseText $BaseDescription
    $current = Split-ContinuationText $CurrentText $CurrentDescription
    if (-not [string]::Equals($base.Historical, $current.Historical, [System.StringComparison]::Ordinal)) {
        throw 'Post-acceptance bookkeeping modified historical CONTINUATION content.'
    }
}

function Assert-Alpha21FailureEvidence([string]$OperativeContinuation) {
    foreach ($required in @(
            '1024/1024',
            'AUDIT FAILED: Post-acceptance bookkeeping modified historical CONTINUATION content.',
            'That progression is a governance failure, not a PASS.',
            'P15 also remains non-compliant because its pasted user input contained assistant-return boundaries')) {
        if ($OperativeContinuation -notmatch [regex]::Escape($required)) {
            throw "Operative continuation is missing required accepted/failure evidence: $required"
        }
    }
}

function Get-SingleDocumentationAuditEntry([string]$BaseText, [string]$CurrentText) {
    $header = "# Documentation Audit Record`n"
    $base = ConvertTo-GovernedText $BaseText
    $current = ConvertTo-GovernedText $CurrentText
    if (-not ($base.StartsWith($header, [System.StringComparison]::Ordinal) -and
            $current.StartsWith($header, [System.StringComparison]::Ordinal))) {
        throw 'Post-acceptance bookkeeping requires the governed DocumentationAudit heading.'
    }

    $baseBody = $base.Substring($header.Length)
    $currentBody = $current.Substring($header.Length)
    $newText = $null
    if ($currentBody.EndsWith($baseBody, [System.StringComparison]::Ordinal)) {
        $newText = $currentBody.Substring(0, $currentBody.Length - $baseBody.Length)
    }
    elseif ($currentBody.StartsWith($baseBody, [System.StringComparison]::Ordinal)) {
        $newText = $currentBody.Substring($baseBody.Length)
    }
    else {
        throw 'Post-acceptance bookkeeping must add one evidence entry without altering existing DocumentationAudit content.'
    }
    if ([regex]::Matches($newText, '(?m)^## ').Count -ne 1) {
        throw 'Post-acceptance bookkeeping must add exactly one DocumentationAudit evidence entry.'
    }
    return $newText
}

function Invoke-GitLines([string]$RepositoryRoot, [string[]]$Arguments) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = @(& git -C $RepositoryRoot @Arguments 2>&1) }
    finally { $ErrorActionPreference = $previousPreference }
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')`n$($output -join "`n")"
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Assert-PostAcceptanceBookkeepingTransition(
    [string]$RepositoryRoot,
    [string]$AcceptedApplicationCommit,
    [string]$BookkeepingCommit) {
    foreach ($sha in @($AcceptedApplicationCommit, $BookkeepingCommit)) {
        if ($sha -cnotmatch '^[0-9a-f]{40}$') { throw "Invalid full commit SHA: $sha" }
        [void](Invoke-GitLines $RepositoryRoot @('cat-file','-e',"$sha^{commit}"))
    }

    $parents = (@(Invoke-GitLines $RepositoryRoot @('show','-s','--format=%P',$BookkeepingCommit))[0]) -split ' '
    if ($parents.Count -ne 1 -or $parents[0] -cne $AcceptedApplicationCommit) {
        throw 'The bookkeeping commit must be the direct, single-parent successor of the accepted application commit.'
    }

    $changed = @(Invoke-GitLines $RepositoryRoot @('diff-tree','--no-commit-id','--name-only','-r',$BookkeepingCommit) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    $expected = @('docs/CONTINUATION.md','docs/DocumentationAudit.md')
    if (($changed -join "`n") -cne ($expected -join "`n")) {
        throw "The bookkeeping commit changed paths outside the exact two-file exception: $($changed -join ', ')."
    }

    $baseContinuation = Get-GitBlobText $RepositoryRoot $AcceptedApplicationCommit 'docs/CONTINUATION.md'
    $bookContinuation = Get-GitBlobText $RepositoryRoot $BookkeepingCommit 'docs/CONTINUATION.md'
    Assert-ContinuationHistoricalPreserved $baseContinuation $bookContinuation 'accepted continuation' 'bookkeeping continuation'
    $bookOperative = (Split-ContinuationText $bookContinuation 'bookkeeping continuation').Operative
    foreach ($required in @(
            'Mode: POST-ACCEPTANCE EVIDENCE BOOKKEEPING',
            'The bookkeeping commit itself was not BAT-tested.',
            $AcceptedApplicationCommit)) {
        if ($bookOperative -notmatch [regex]::Escape($required)) {
            throw "The bookkeeping continuation is missing required evidence: $required"
        }
    }

    $baseAudit = Get-GitBlobText $RepositoryRoot $AcceptedApplicationCommit 'docs/DocumentationAudit.md'
    $bookAudit = Get-GitBlobText $RepositoryRoot $BookkeepingCommit 'docs/DocumentationAudit.md'
    $entry = Get-SingleDocumentationAuditEntry $baseAudit $bookAudit
    foreach ($required in @(
            'Mode: POST-ACCEPTANCE EVIDENCE BOOKKEEPING',
            'The bookkeeping commit itself was not BAT-tested.',
            $AcceptedApplicationCommit,
            '1024/1024')) {
        if ($entry -notmatch [regex]::Escape($required)) {
            throw "The bookkeeping audit entry is missing required evidence: $required"
        }
    }
}

function Get-Sha256Text([string]$Text) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = (Get-StrictUtf8Encoding).GetBytes($Text)
        return ([BitConverter]::ToString($algorithm.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()
    }
    finally { $algorithm.Dispose() }
}

function Get-GitCanonicalCandidateManifest(
    [string]$RepositoryRoot,
    [string[]]$Paths) {
    $records = @()
    foreach ($path in @($Paths | Sort-Object -Unique)) {
        if ($path -cnotmatch '^[A-Za-z0-9._/-]+$') { throw "Unsafe candidate path: $path" }
        $fullPath = Join-Path $RepositoryRoot ($path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            throw "Candidate path is missing or is not a file: $path"
        }

        $tracked = @(Invoke-GitLines $RepositoryRoot @('ls-files','--stage','--',$path))
        if ($tracked.Count -gt 1) { throw "Candidate path has unresolved index stages: $path" }
        $mode = if ($tracked.Count -eq 1 -and $tracked[0] -match '^(?<mode>[0-9]{6}) ') {
            $matches['mode']
        }
        else { '100644' }
        $objectId = (@(Invoke-GitLines $RepositoryRoot @('hash-object',"--path=$path",'--',$path))[0]).Trim()
        if ($objectId -cnotmatch '^[0-9a-f]{40,64}$') { throw "Invalid Git object identity for candidate path: $path" }
        $records += "$mode $objectId`t$path"
    }
    $text = ($records -join "`n") + "`n"
    return [pscustomobject]@{
        Records = @($records)
        Text = $text
        Sha256 = Get-Sha256Text $text
    }
}

function Get-GitIndexManifest(
    [string]$RepositoryRoot,
    [string[]]$ExpectedPaths) {
    $expected = @($ExpectedPaths | Sort-Object -Unique)
    $actualPaths = @(Invoke-GitLines $RepositoryRoot @('diff','--cached','--name-only') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    if (($actualPaths -join "`n") -cne ($expected -join "`n")) {
        throw "Staged paths differ from the approved candidate: $($actualPaths -join ', ')."
    }

    $records = @()
    foreach ($path in $expected) {
        $stageLines = @(Invoke-GitLines $RepositoryRoot @('ls-files','--stage','--',$path))
        if ($stageLines.Count -ne 1 -or
            $stageLines[0] -cnotmatch '^(?<mode>[0-9]{6}) (?<object>[0-9a-f]{40,64}) 0\t(?<path>.+)$' -or
            $matches['path'] -cne $path) {
            throw "Staged path does not have one stage-zero Git blob: $path"
        }
        $records += "$($matches['mode']) $($matches['object'])`t$path"
    }
    $text = ($records -join "`n") + "`n"
    return [pscustomobject]@{
        Records = @($records)
        Text = $text
        Sha256 = Get-Sha256Text $text
    }
}

function Assert-GitIndexMatchesApprovedCandidate(
    [string]$RepositoryRoot,
    [string[]]$ExpectedPaths,
    [string]$ExpectedManifestSha256) {
    if ($ExpectedManifestSha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Approved candidate manifest SHA-256 is invalid.'
    }
    $manifest = Get-GitIndexManifest $RepositoryRoot $ExpectedPaths
    if ($manifest.Sha256 -cne $ExpectedManifestSha256) {
        throw "Staged Git blobs do not match the approved candidate bytes. Actual manifest SHA-256: $($manifest.Sha256)"
    }
    return $manifest
}

function Assert-DevelopmentLineage(
    [string]$RepositoryRoot,
    [string]$RequiredAncestor,
    [string[]]$ApprovedMergeCommits = @()) {
    if ($RequiredAncestor -cnotmatch '^[0-9a-f]{40}$') { throw 'Required ancestor must be a full commit SHA.' }
    [void](Invoke-GitLines $RepositoryRoot @('cat-file','-e',"$RequiredAncestor^{commit}"))

    $branch = (@(Invoke-GitLines $RepositoryRoot @('branch','--show-current'))[0]).Trim()
    $head = (@(Invoke-GitLines $RepositoryRoot @('rev-parse','HEAD'))[0]).Trim()
    & git -C $RepositoryRoot merge-base --is-ancestor $RequiredAncestor $head 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'Current HEAD is not descended from the verified bookkeeping transition.'
    }

    $approved = @($ApprovedMergeCommits | Sort-Object -Unique)
    foreach ($sha in $approved) {
        if ($sha -cnotmatch '^[0-9a-f]{40}$') { throw "Invalid reviewed reintegration commit SHA: $sha" }
    }
    $merges = @(Invoke-GitLines $RepositoryRoot @('rev-list','--merges',"$RequiredAncestor..$head") |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    $unapproved = @($merges | Where-Object { $approved -cnotcontains $_ })
    $forgedApprovals = @($approved | Where-Object { $merges -cnotcontains $_ })
    if ($unapproved.Count -ne 0) {
        throw "Current lineage contains merge commits without recorded reviewed reintegration: $($unapproved -join ', ')."
    }
    if ($forgedApprovals.Count -ne 0) {
        throw "Recorded reviewed reintegration commits are not merges in current ancestry: $($forgedApprovals -join ', ')."
    }

    $upstream = $null
    $ahead = $null
    $behind = $null
    $upstreamLines = @()
    try { $upstreamLines = @(Invoke-GitLines $RepositoryRoot @('rev-parse','@{upstream}')) }
    catch { $upstreamLines = @() }
    if ($upstreamLines.Count -eq 1) {
        $upstream = $upstreamLines[0].Trim()
        $divergence = ((@(Invoke-GitLines $RepositoryRoot @('rev-list','--left-right','--count','HEAD...@{upstream}'))[0]) -split '\s+')
        if ($divergence.Count -eq 2) {
            $ahead = [int]$divergence[0]
            $behind = [int]$divergence[1]
        }
    }
    return [pscustomobject]@{
        Branch = $branch
        Head = $head
        Upstream = $upstream
        Ahead = $ahead
        Behind = $behind
        ReviewedMerges = @($merges)
    }
}
