$ErrorActionPreference = "Stop"
dotnet restore .\BinTracker.sln
dotnet build .\BinTracker.sln --configuration Debug --no-restore
Write-Host ""
Write-Host "Build succeeded."
