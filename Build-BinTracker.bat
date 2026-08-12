@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "VERSION=unknown"

for /f "usebackq delims=" %%V in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; [xml]$p=Get-Content -LiteralPath 'Directory.Build.props'; [string]$v=$p.Project.PropertyGroup.Version; if([string]::IsNullOrWhiteSpace($v)){throw 'Version not found'}; $v.Trim()" 2^>nul`) do (
    set "VERSION=%%V"
)

echo ==========================================================
echo  BinTracker Build
echo  Version       : v%VERSION%
echo  Configuration : Debug
echo ==========================================================
echo.

echo Restoring BinTracker...
dotnet restore .\BinTracker.sln
if errorlevel 1 goto :fail

echo.
echo Building BinTracker v%VERSION%...
dotnet build .\BinTracker.sln --configuration Debug --no-restore
if errorlevel 1 goto :fail

echo.
echo Running automated tests...
dotnet test .\BinTracker.sln --configuration Debug --no-build
if errorlevel 1 goto :fail

echo.
echo ==========================================================
echo  BUILD SUCCESSFUL
echo  BinTracker v%VERSION%
echo ==========================================================
exit /b 0

:fail
echo.
echo ==========================================================
echo  BUILD FAILED
echo  BinTracker v%VERSION%
echo ==========================================================
exit /b 1
