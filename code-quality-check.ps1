#!/usr/bin/env pwsh

# Classic Emoji Picker - Code Quality Check Script
# Equivalent to cppcheck for C# projects

Write-Host "🔍 Classic Emoji Picker - Code Quality Check" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$ErrorCount = 0

# Check if we're in the right directory
if (-not (Test-Path "EmojiPicker\EmojiPicker.csproj")) {
    Write-Host "❌ Error: Must be run from the Classic-EmojiPicker root directory" -ForegroundColor Red
    exit 1
}

Write-Host "📂 Working Directory: $(Get-Location)" -ForegroundColor Green
Write-Host ""

# 1. Build Check
Write-Host "🔨 Building project..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    $ErrorCount++
} else {
    Write-Host "✅ Build successful" -ForegroundColor Green
}
Write-Host ""

# 2. Code Analysis
Write-Host "📊 Running code analysis..." -ForegroundColor Yellow
$analysisResult = dotnet build --configuration Release --verbosity normal --no-restore 2>&1
$analysisWarnings = $analysisResult | Select-String -Pattern "warning"
$analysisErrors = $analysisResult | Select-String -Pattern "error"

if ($analysisErrors.Count -gt 0) {
    Write-Host "❌ Code analysis found errors:" -ForegroundColor Red
    $analysisErrors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $ErrorCount++
} elseif ($analysisWarnings.Count -gt 0) {
    Write-Host "⚠️  Code analysis found warnings:" -ForegroundColor Yellow
    $analysisWarnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
} else {
    Write-Host "✅ Code analysis passed" -ForegroundColor Green
}
Write-Host ""

# 3. Format Check
Write-Host "📋 Checking code formatting..." -ForegroundColor Yellow
$formatResult = dotnet format --verify-no-changes --verbosity diagnostic 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Code formatting issues found!" -ForegroundColor Red
    Write-Host "Run 'dotnet format' to fix formatting issues" -ForegroundColor Red
    $ErrorCount++
} else {
    Write-Host "✅ Code formatting is correct" -ForegroundColor Green
}
Write-Host ""

# 4. File Encoding Check
Write-Host "📝 Checking file encoding..." -ForegroundColor Yellow
$csharpFiles = Get-ChildItem -Path "EmojiPicker" -Filter "*.cs" -Recurse
$encodingIssues = @()

foreach ($file in $csharpFiles) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    if ($content -match [char]0xFEFF) {
        $encodingIssues += $file.FullName
    }
}

if ($encodingIssues.Count -gt 0) {
    Write-Host "⚠️  Files with BOM found:" -ForegroundColor Yellow
    $encodingIssues | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
} else {
    Write-Host "✅ File encoding check passed" -ForegroundColor Green
}
Write-Host ""

# 5. Performance Check (basic)
Write-Host "⚡ Basic performance analysis..." -ForegroundColor Yellow
$perfIssues = @()

# Check for common performance issues
$csharpFiles | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw
    $lineNum = 1
    Get-Content -Path $_.FullName | ForEach-Object {
        if ($_ -match 'string\.Concat\(' -or $_ -match '\+.*\+.*string') {
            $perfIssues += "$($_.FullName):$lineNum - Consider using StringBuilder for multiple string concatenations"
        }
        if ($_ -match '\.ToList\(\)\.Count' -or $_ -match '\.ToArray\(\)\.Length') {
            $perfIssues += "$($_.FullName):$lineNum - Use .Count() instead of .ToList().Count"
        }
        $lineNum++
    }
}

if ($perfIssues.Count -gt 0) {
    Write-Host "⚠️  Potential performance issues:" -ForegroundColor Yellow
    $perfIssues | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
} else {
    Write-Host "✅ No obvious performance issues found" -ForegroundColor Green
}
Write-Host ""

# 6. Security Check (basic)
Write-Host "🔒 Basic security analysis..." -ForegroundColor Yellow
$securityIssues = @()

$csharpFiles | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw
    if ($content -match 'System\.Diagnostics\.Process\.Start\(' -and $content -notmatch 'ProcessStartInfo') {
        $securityIssues += "$($_.FullName) - Consider using ProcessStartInfo for Process.Start"
    }
    if ($content -match 'File\.ReadAllText\(' -and $content -notmatch 'try.*catch') {
        $securityIssues += "$($_.FullName) - File operations should be wrapped in try-catch"
    }
}

if ($securityIssues.Count -gt 0) {
    Write-Host "⚠️  Security considerations:" -ForegroundColor Yellow
    $securityIssues | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
} else {
    Write-Host "✅ No obvious security issues found" -ForegroundColor Green
}
Write-Host ""

# Summary
Write-Host "📋 Summary" -ForegroundColor Cyan
Write-Host "==========" -ForegroundColor Cyan
if ($ErrorCount -eq 0) {
    Write-Host "🎉 All checks passed! Code quality is good." -ForegroundColor Green
} else {
    Write-Host "❌ $ErrorCount issue(s) found. Please review and fix." -ForegroundColor Red
}

Write-Host ""
Write-Host "💡 Tips:" -ForegroundColor Blue
Write-Host "  - Run 'dotnet format' to auto-fix formatting issues" -ForegroundColor Blue
Write-Host "  - Use 'dotnet build --verbosity normal' for detailed analysis" -ForegroundColor Blue
Write-Host "  - Consider adding more emojis to reach feature parity" -ForegroundColor Blue

exit $ErrorCount
