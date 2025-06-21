#!/usr/bin/env pwsh

# Classic Emoji Picker - Code Quality Check Script
# Equivalent to cppcheck for C# projects

Write-Host "Classic Emoji Picker - Code Quality Check" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorCount = 0

# Check if we're in the right directory
if (-not (Test-Path "EmojiPicker\EmojiPicker.csproj")) {
    Write-Host "Error: Must be run from the Classic-EmojiPicker root directory" -ForegroundColor Red
    exit 1
}

Write-Host "Working Directory: $(Get-Location)" -ForegroundColor Green
Write-Host ""

# 1. Build Check
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity quiet | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    $ErrorCount++
} else {
    Write-Host "Build successful" -ForegroundColor Green
}
Write-Host ""

# 2. Code Analysis
Write-Host "Running code analysis..." -ForegroundColor Yellow
$analysisResult = dotnet build --configuration Release --verbosity normal --no-restore 2>&1
$analysisWarnings = $analysisResult | Select-String -Pattern "warning" | Where-Object { $_.Line -notmatch "0 Warning\(s\)" }
$analysisErrors = $analysisResult | Select-String -Pattern "error" | Where-Object { $_.Line -notmatch "0 Error\(s\)" }

if ($analysisErrors.Count -gt 0) {
    Write-Host "Code analysis found errors:" -ForegroundColor Red
    $analysisErrors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $ErrorCount++
} elseif ($analysisWarnings.Count -gt 0) {
    Write-Host "Code analysis found warnings:" -ForegroundColor Yellow
    $analysisWarnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
} else {
    Write-Host "Code analysis passed" -ForegroundColor Green
}
Write-Host ""

# 3. Format Check
Write-Host "Checking code formatting..." -ForegroundColor Yellow
dotnet format --verify-no-changes --verbosity quiet | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Code formatting issues found!" -ForegroundColor Red
    Write-Host "Run 'dotnet format' to fix formatting issues" -ForegroundColor Red
    $ErrorCount++
} else {
    Write-Host "Code formatting is correct" -ForegroundColor Green
}
Write-Host ""

# Summary
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "=======" -ForegroundColor Cyan
if ($ErrorCount -eq 0) {
    Write-Host "All checks passed! Code quality is good." -ForegroundColor Green
} else {
    Write-Host "$ErrorCount issue(s) found. Please review and fix." -ForegroundColor Red
}

Write-Host ""
Write-Host "Tips:" -ForegroundColor Blue
Write-Host "  - Run 'dotnet format' to auto-fix formatting issues" -ForegroundColor Blue
Write-Host "  - Use 'dotnet build --verbosity normal' for detailed analysis" -ForegroundColor Blue

exit $ErrorCount
