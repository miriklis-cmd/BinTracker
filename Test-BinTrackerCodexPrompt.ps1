param(
    [string]$PromptText,
    [string]$HandoffText,
    [string]$SessionJsonlPath,
    [string]$TaskId,
    [switch]$ValidateHandoff,
    [switch]$SelfTest,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$WorkingStartPattern = '^=== BINTRACKER CODEX WORKING: (?<id>[A-Z0-9-]+) START ===$'
$ReturnStartPattern = '^=== RETURN TO CHATGPT: (?<id>[A-Z0-9-]+) START ===$'
$ReturnEndPattern = '^=== RETURN TO CHATGPT: (?<id>[A-Z0-9-]+) END ===$'
$WorkingEndPattern = '^=== BINTRACKER CODEX WORKING: (?<id>[A-Z0-9-]+) END ===$'

function Test-CanonicalTaskId([string]$candidate) {
    return $candidate -cmatch '^BT-[A-Z0-9]+(?:-(?:P|FIX)[1-9][0-9]*)?$'
}

function Get-FirstNonBlankLine([string]$text) {
    $lines = @($text -split "`r?`n" | Where-Object { $_.Trim().Length -gt 0 })
    if ($lines.Count -eq 0) { return '' }
    return $lines[0]
}

function Get-ApplicableBoundaryMarkers([string]$text) {
    $insideFence = $false
    $markers = @()
    $lineNumber = 0
    foreach ($line in ($text -split "`r?`n")) {
        $lineNumber++
        if ($line -match '^\s*(?:```|~~~)') {
            $insideFence = -not $insideFence
            continue
        }
        if ($insideFence -or $line -match '^\s*>') { continue }

        $kind = $null
        $id = $null
        if ($line -cmatch $WorkingStartPattern) { $kind = 'WorkingStart'; $id = $matches['id'] }
        elseif ($line -cmatch $ReturnStartPattern) { $kind = 'ReturnStart'; $id = $matches['id'] }
        elseif ($line -cmatch $ReturnEndPattern) { $kind = 'ReturnEnd'; $id = $matches['id'] }
        elseif ($line -cmatch $WorkingEndPattern) { $kind = 'WorkingEnd'; $id = $matches['id'] }
        if ($null -ne $kind) {
            $markers += [pscustomobject]@{ Kind = $kind; TaskId = $id; LineNumber = $lineNumber; Text = $line }
        }
    }
    return @($markers)
}

function New-ValidationResult([bool]$valid, [string[]]$errors, [object[]]$markers) {
    return [pscustomobject]@{
        IsValid = $valid
        Errors = @($errors)
        Markers = @($markers)
    }
}

function Test-TaskInput([string]$text, [string]$expectedTaskId) {
    $errors = @()
    if (-not (Test-CanonicalTaskId $expectedTaskId)) {
        $errors += "Invalid canonical BinTracker task ID '$expectedTaskId'. Use BT-<roadmap-id>, -P<n>, or -FIX<n>; R<n> is reserved."
    }

    $expectedOpening = "=== BINTRACKER CODEX WORKING: $expectedTaskId START ==="
    $firstLine = Get-FirstNonBlankLine $text
    if ($firstLine -cne $expectedOpening) {
        $errors += "The task must begin with '$expectedOpening'; its first non-blank line is '$firstLine'."
    }

    $markers = @(Get-ApplicableBoundaryMarkers $text)
    $openings = @($markers | Where-Object Kind -eq 'WorkingStart')
    if ($openings.Count -ne 1) {
        $errors += "The input/task must contain exactly one applicable working START boundary; found $($openings.Count)."
    }
    elseif ($openings[0].TaskId -cne $expectedTaskId) {
        $errors += "The input/task working START ID '$($openings[0].TaskId)' does not match '$expectedTaskId'."
    }

    return New-ValidationResult ($errors.Count -eq 0) $errors $openings
}

function Test-ReturnedHandoffShape([string]$text, [string]$expectedTaskId) {
    $errors = @()
    if (-not (Test-CanonicalTaskId $expectedTaskId)) {
        $errors += "Invalid canonical BinTracker task ID '$expectedTaskId'. Use BT-<roadmap-id>, -P<n>, or -FIX<n>; R<n> is reserved."
    }

    $expectedReturnStart = "=== RETURN TO CHATGPT: $expectedTaskId START ==="
    $firstLine = Get-FirstNonBlankLine $text
    if ($firstLine -cne $expectedReturnStart) {
        $errors += "The returned handoff must begin with '$expectedReturnStart'; its first non-blank line is '$firstLine'."
    }

    $markers = @(Get-ApplicableBoundaryMarkers $text)
    $expectedKinds = @('ReturnStart', 'ReturnEnd', 'WorkingEnd')
    foreach ($kind in @('WorkingStart', 'ReturnStart', 'ReturnEnd', 'WorkingEnd')) {
        $count = @($markers | Where-Object Kind -eq $kind).Count
        $expectedCount = if ($kind -eq 'WorkingStart') { 0 } else { 1 }
        if ($count -ne $expectedCount) {
            $errors += "The returned handoff must contain exactly $expectedCount applicable $kind boundary; found $count."
        }
    }

    $actualKinds = @($markers | ForEach-Object Kind)
    if (($actualKinds -join ',') -cne ($expectedKinds -join ',')) {
        $errors += "Returned-handoff boundary order must be ReturnStart, ReturnEnd, WorkingEnd; found $($actualKinds -join ', ')."
    }
    foreach ($marker in $markers) {
        if ($marker.TaskId -cne $expectedTaskId) {
            $errors += "Returned-handoff $($marker.Kind) ID '$($marker.TaskId)' does not match '$expectedTaskId'."
        }
    }

    $nonBlank = @($text -split "`r?`n" | Where-Object { $_.Trim().Length -gt 0 })
    if ($nonBlank.Count -lt 2 -or
        $nonBlank[-2] -cne "=== RETURN TO CHATGPT: $expectedTaskId END ===" -or
        $nonBlank[-1] -cne "=== BINTRACKER CODEX WORKING: $expectedTaskId END ===") {
        $errors += 'The returned handoff must end with the matching return END and working END boundaries, in that order.'
    }

    return New-ValidationResult ($errors.Count -eq 0) $errors $markers
}

function Test-CompleteEnvelope([string]$taskText, [string]$returnedText, [string]$expectedTaskId) {
    $input = Test-TaskInput $taskText $expectedTaskId
    $outputShape = Test-ReturnedHandoffShape $returnedText $expectedTaskId
    $errors = @()
    if (-not $input.IsValid) { $errors += @($input.Errors | ForEach-Object { "INPUT/TASK: $_" }) }
    if (-not $outputShape.IsValid) { $errors += @($outputShape.Errors | ForEach-Object { "OUTPUT/HANDOFF: $_" }) }

    $combinedMarkers = @($input.Markers) + @($outputShape.Markers)
    $expectedKinds = @('WorkingStart', 'ReturnStart', 'ReturnEnd', 'WorkingEnd')
    $actualKinds = @($combinedMarkers | ForEach-Object Kind)
    if (($actualKinds -join ',') -cne ($expectedKinds -join ',')) {
        $errors += "OUTPUT/HANDOFF: the role-separated four-boundary envelope must be WorkingStart, ReturnStart, ReturnEnd, WorkingEnd; found $($actualKinds -join ', ')."
    }
    foreach ($kind in $expectedKinds) {
        $count = @($combinedMarkers | Where-Object Kind -eq $kind).Count
        if ($count -ne 1) {
            $errors += "OUTPUT/HANDOFF: the complete envelope requires exactly one $kind boundary; found $count."
        }
    }

    return [pscustomobject]@{
        IsValid = $errors.Count -eq 0
        InputTaskValid = $input.IsValid
        OutputHandoffValid = $input.IsValid -and $outputShape.IsValid -and $errors.Count -eq 0
        OutputShapeValid = $outputShape.IsValid
        Errors = @($errors)
        TaskId = $expectedTaskId
        BoundaryCount = $combinedMarkers.Count
    }
}

function Assert-SelfTest([string]$name, [bool]$condition) {
    if (-not $condition) { throw "Marker self-test '$name' failed." }
    if (-not $Quiet) { Write-Host "PASS $name" -ForegroundColor Green }
}

function Invoke-SelfTests {
    $id = 'BT-20F-FIX3'
    $validTask = "=== BINTRACKER CODEX WORKING: $id START ===`nwork"
    $validHandoff = "=== RETURN TO CHATGPT: $id START ===`nhandoff`n=== RETURN TO CHATGPT: $id END ===`n=== BINTRACKER CODEX WORKING: $id END ==="

    Assert-SelfTest 'valid-four-boundary-envelope' (Test-CompleteEnvelope $validTask $validHandoff $id).IsValid
    Assert-SelfTest 'missing-working-start' (-not (Test-CompleteEnvelope 'work' $validHandoff $id).IsValid)
    Assert-SelfTest 'missing-return-start' (-not (Test-CompleteEnvelope $validTask "handoff`n=== RETURN TO CHATGPT: $id END ===`n=== BINTRACKER CODEX WORKING: $id END ===" $id).IsValid)
    Assert-SelfTest 'missing-return-end' (-not (Test-CompleteEnvelope $validTask "=== RETURN TO CHATGPT: $id START ===`nhandoff`n=== BINTRACKER CODEX WORKING: $id END ===" $id).IsValid)
    Assert-SelfTest 'missing-working-end' (-not (Test-CompleteEnvelope $validTask "=== RETURN TO CHATGPT: $id START ===`nhandoff`n=== RETURN TO CHATGPT: $id END ===" $id).IsValid)
    Assert-SelfTest 'mismatched-task-ids' (-not (Test-CompleteEnvelope $validTask ($validHandoff -replace 'RETURN TO CHATGPT: BT-20F-FIX3 END', 'RETURN TO CHATGPT: BT-20F-P3 END') $id).IsValid)
    Assert-SelfTest 'wrong-marker-order' (-not (Test-CompleteEnvelope $validTask "=== RETURN TO CHATGPT: $id START ===`n=== BINTRACKER CODEX WORKING: $id END ===`n=== RETURN TO CHATGPT: $id END ===" $id).IsValid)
    Assert-SelfTest 'duplicated-marker' (-not (Test-CompleteEnvelope $validTask "=== RETURN TO CHATGPT: $id START ===`n=== RETURN TO CHATGPT: $id START ===`n=== RETURN TO CHATGPT: $id END ===`n=== BINTRACKER CODEX WORKING: $id END ===" $id).IsValid)
    Assert-SelfTest 'prohibited-r-suffix' (-not (Test-CompleteEnvelope "=== BINTRACKER CODEX WORKING: BT-20F-R3 START ===" "=== RETURN TO CHATGPT: BT-20F-R3 START ===`n=== RETURN TO CHATGPT: BT-20F-R3 END ===`n=== BINTRACKER CODEX WORKING: BT-20F-R3 END ===" 'BT-20F-R3').IsValid)
    Assert-SelfTest 'valid-input-invalid-returned-handoff' (-not (Test-CompleteEnvelope $validTask 'handoff only' $id).IsValid)
    $invalidInputResult = Test-CompleteEnvelope 'quoted example only: === BINTRACKER CODEX WORKING: BT-20F-FIX3 START ===' $validHandoff $id
    Assert-SelfTest 'valid-returned-handoff-invalid-input-identity' (-not $invalidInputResult.IsValid -and $invalidInputResult.OutputShapeValid)
    $quotedOnly = "intro`n" + '```text' +
        "`n=== BINTRACKER CODEX WORKING: $id START ===`n" + '```'
    Assert-SelfTest 'quoted-marker-example-does-not-prove-input' (-not (Test-TaskInput $quotedOnly $id).IsValid)
    if (-not $Quiet) { Write-Host 'BinTracker Codex four-boundary validator self-test passed: 12/12.' -ForegroundColor Green }
}

function Get-MessageText([object]$record) {
    return (($record.payload.content | ForEach-Object { $_.text }) -join "`n")
}

if ($SelfTest) {
    Invoke-SelfTests
    exit 0
}

if ([string]::IsNullOrWhiteSpace($TaskId)) {
    throw 'TaskId is required unless -SelfTest is used.'
}
if (-not [string]::IsNullOrWhiteSpace($SessionJsonlPath)) {
    if (-not [string]::IsNullOrWhiteSpace($PromptText) -or -not [string]::IsNullOrWhiteSpace($HandoffText)) {
        throw 'Use either -SessionJsonlPath or explicit -PromptText/-HandoffText, not both.'
    }
    if (-not (Test-Path -LiteralPath $SessionJsonlPath -PathType Leaf)) {
        throw "Session JSONL not found: $SessionJsonlPath"
    }

    $records = @(Get-Content -LiteralPath $SessionJsonlPath | ForEach-Object { $_ | ConvertFrom-Json })
    $candidateTasks = @($records | Where-Object {
        $_.type -eq 'response_item' -and $_.payload.type -eq 'message' -and $_.payload.role -eq 'user'
    } | ForEach-Object { Get-MessageText $_ } | Where-Object {
        (Get-FirstNonBlankLine $_) -cmatch $WorkingStartPattern
    })
    if ($candidateTasks.Count -ne 1) {
        throw "INPUT/TASK VALIDATION FAILED: session JSONL must contain exactly one attributable user task beginning with a working START boundary; found $($candidateTasks.Count)."
    }
    $PromptText = $candidateTasks[0]

    if ($ValidateHandoff) {
        $candidateHandoffs = @($records | Where-Object {
            $_.type -eq 'response_item' -and $_.payload.type -eq 'message' -and $_.payload.role -eq 'assistant'
        } | ForEach-Object { Get-MessageText $_ } | Where-Object {
            (Get-FirstNonBlankLine $_) -cmatch $ReturnStartPattern
        })
        if ($candidateHandoffs.Count -ne 1) {
            throw "OUTPUT/HANDOFF VALIDATION FAILED: session JSONL must contain exactly one attributable assistant handoff beginning with a return START boundary; found $($candidateHandoffs.Count)."
        }
        $HandoffText = $candidateHandoffs[0]
    }
}

if ([string]::IsNullOrWhiteSpace($PromptText)) { throw 'PromptText is required.' }

if ($ValidateHandoff -or -not [string]::IsNullOrWhiteSpace($HandoffText)) {
    if ([string]::IsNullOrWhiteSpace($HandoffText)) { throw 'HandoffText is required for output/handoff validation.' }
    $result = Test-CompleteEnvelope $PromptText $HandoffText $TaskId
    if (-not $Quiet) {
        Write-Host "INPUT/TASK VALIDATION: $(if ($result.InputTaskValid) { 'PASSED' } else { 'FAILED' })"
        Write-Host "OUTPUT/HANDOFF VALIDATION: $(if ($result.OutputHandoffValid) { 'PASSED' } else { 'FAILED' })"
        foreach ($error in $result.Errors) { Write-Host $error -ForegroundColor Red }
    }
    if (-not $result.IsValid) { exit 1 }
    if (-not $Quiet) { $result }
    exit 0
}

$inputResult = Test-TaskInput $PromptText $TaskId
if (-not $Quiet) {
    Write-Host "INPUT/TASK VALIDATION: $(if ($inputResult.IsValid) { 'PASSED' } else { 'FAILED' })"
    Write-Host 'OUTPUT/HANDOFF VALIDATION: NOT EVALUATED'
    foreach ($error in $inputResult.Errors) { Write-Host $error -ForegroundColor Red }
}
if (-not $inputResult.IsValid) { exit 1 }
