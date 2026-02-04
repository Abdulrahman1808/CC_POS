# POS System - Self-Contained Release Build Script
# This script creates a standalone executable that does NOT require .NET 8 to be installed

Write-Host "=== POS System Self-Contained Build ===" -ForegroundColor Green
Write-Host ""

# Config
$projectPath = $PSScriptRoot
$outputPath = "$projectPath\publish\win-x64"

Write-Host "Project Path: $projectPath"
Write-Host "Output Path: $outputPath"
Write-Host ""

# Clean previous builds
if (Test-Path $outputPath) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $outputPath
}

# Build self-contained single-file executable
Write-Host "Building self-contained release..." -ForegroundColor Cyan

dotnet publish `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $outputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== BUILD SUCCESSFUL ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Output location:" -ForegroundColor White
    Write-Host "  $outputPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Files created:" -ForegroundColor White
    Get-ChildItem $outputPath | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 2)
        Write-Host "  $($_.Name) - $size MB" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "READY TO DISTRIBUTE!" -ForegroundColor Green
    Write-Host "The tester can run POSSystem.exe without installing .NET 8."
    
    # Open output folder
    explorer $outputPath
}
else {
    Write-Host "=== BUILD FAILED ===" -ForegroundColor Red
    exit 1
}
