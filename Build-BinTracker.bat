@echo off
setlocal

echo Restoring BinTracker...
dotnet restore .\BinTracker.sln
if errorlevel 1 goto :fail

echo Building BinTracker...
dotnet build .\BinTracker.sln --configuration Debug --no-restore
if errorlevel 1 goto :fail

echo Running automated tests...
dotnet test .\BinTracker.sln --configuration Debug --no-build
if errorlevel 1 goto :fail

echo.
echo Build and tests succeeded.
exit /b 0

:fail
echo.
echo Build or tests FAILED.
exit /b 1
