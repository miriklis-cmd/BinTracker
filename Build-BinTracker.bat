@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "VERSION=unknown"
set "DOTNET_CLI_USE_MSBUILD_SERVER=0"
set "MSBUILDDISABLENODEREUSE=1"

for /f "usebackq delims=" %%V in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; [xml]$p=Get-Content -LiteralPath 'Directory.Build.props'; [string]$v=$p.Project.PropertyGroup.Version; if([string]::IsNullOrWhiteSpace($v)){throw 'Version not found'}; $v.Trim()" 2^>nul`) do (
    set "VERSION=%%V"
)

echo ==========================================================
echo  BinTracker Build
echo  Version       : v%VERSION%
echo  Configuration : Debug
echo ==========================================================
echo.

echo Running BinTracker source/package-state audit...
powershell -NoProfile -ExecutionPolicy Bypass -File .\Audit-BinTracker.ps1 || goto :fail
echo.

where dotnet >nul 2>&1 || (
    echo ERROR: dotnet was not found on PATH.
    goto :fail
)

for /f "usebackq delims=" %%S in (`dotnet --version 2^>nul`) do set "DOTNET_SDK=%%S"

if not defined DOTNET_SDK (
    echo ERROR: No usable .NET SDK could be resolved.
    goto :fail
)

echo .NET SDK      : %DOTNET_SDK%
echo.

echo Shutting down stale .NET/MSBuild build servers...
dotnet build-server shutdown >nul 2>&1
rem Shutdown failure is non-fatal.

echo Running locked dependency, advisory and code-security checks...
powershell -NoProfile -ExecutionPolicy Bypass -File .\Invoke-BinTrackerSecurityChecks.ps1 || goto :fail

echo.
echo Building BinTracker v%VERSION%...
dotnet build .\BinTracker.sln --configuration Debug --no-restore -m:1 /nr:false || goto :fail

echo.
echo Running automated tests...
dotnet test .\BinTracker.sln --configuration Debug --no-build --no-restore -m:1 /nr:false || goto :fail

echo.
echo ==========================================================
echo  BUILD SUCCESSFUL
echo  BinTracker v%VERSION%
echo  .NET SDK %DOTNET_SDK%
echo ==========================================================
exit /b 0

:fail
echo.
echo ==========================================================
echo  BUILD FAILED
echo  BinTracker v%VERSION%
if defined DOTNET_SDK echo  .NET SDK %DOTNET_SDK%
echo ==========================================================
exit /b 1
