#!/usr/bin/env pwsh
# Test script for next-gen CI workflow

Write-Host "Testing next-gen-ci workflow locally..." -ForegroundColor Green

# Check if we're in the right directory
if (-not (Test-Path "next-gen")) {
    Write-Host "❌ Error: next-gen folder not found. Are you in the repo root?" -ForegroundColor Red
    exit 1
}

# Check if required files exist
$requiredFiles = @(
    "next-gen/global.json",
    "next-gen/next-gen.sln",
    ".github/workflows/next-gen-ci.yml"
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-Host "❌ Error: Required file not found: $file" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "✅ Found: $file" -ForegroundColor Green
    }
}

# Test the workflow syntax
Write-Host "`n🔍 Testing workflow syntax..." -ForegroundColor Yellow
try {
    $result = act -W .github/workflows/next-gen-ci.yml --list 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Workflow syntax is valid" -ForegroundColor Green
    } else {
        Write-Host "❌ Workflow syntax error:" -ForegroundColor Red
        Write-Host $result
        exit 1
    }
} catch {
    Write-Host "❌ Error running act: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test dry run of each job
$jobs = @("build-and-test", "code-quality", "security-scan", "summary")

foreach ($job in $jobs) {
    Write-Host "`n🧪 Testing job: $job" -ForegroundColor Yellow
    try {
        $result = act -W .github/workflows/next-gen-ci.yml -j $job -n 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Job $job dry-run successful" -ForegroundColor Green
        } else {
            Write-Host "❌ Job $job dry-run failed:" -ForegroundColor Red
            Write-Host $result
        }
    } catch {
        Write-Host "❌ Error testing job $job`: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n🎉 Testing completed!" -ForegroundColor Green
Write-Host "To run a specific job for real, use:" -ForegroundColor Cyan
Write-Host "  act -W .github/workflows/next-gen-ci.yml -j <job-name>" -ForegroundColor Cyan
Write-Host "`nAvailable jobs: build-and-test, code-quality, security-scan, summary" -ForegroundColor Cyan
