$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Description,

        [Parameter(Mandatory = $true)]
        [scriptblock] $Command
    )

    Write-Host $Description
    & $Command

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "FAILED: $Description" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Invoke-Step "Restoring BinTracker..." { dotnet restore .\BinTracker.sln }
Invoke-Step "Building BinTracker..." { dotnet build .\BinTracker.sln --configuration Debug --no-restore }
Invoke-Step "Running automated tests..." { dotnet test .\BinTracker.sln --configuration Debug --no-build }

Write-Host ""
Write-Host "Build and tests succeeded." -ForegroundColor Green
