$ErrorActionPreference = "Stop"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    throw "MSBuild.exe not found at $msbuild"
}

Write-Host "[1/2] Building Portable Edition (Windows Desktop Enhance.exe)..." -ForegroundColor Cyan
& $msbuild "$PSScriptRoot\src\WindowsDesktopEnhance.csproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[2/2] Building Installer Edition (Windows Desktop Enhance Setup.exe)..." -ForegroundColor Cyan
& $msbuild "$PSScriptRoot\installer\WindowsDesktopEnhance.Setup.csproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Build succeeded!" -ForegroundColor Green
Write-Host "  Portable  : $(Join-Path $PSScriptRoot 'Windows Desktop Enhance.exe')" -ForegroundColor Green
Write-Host "  Installer : $(Join-Path $PSScriptRoot 'Windows Desktop Enhance Setup.exe')" -ForegroundColor Green
