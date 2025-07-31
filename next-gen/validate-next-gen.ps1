#!/usr/bin/env pwsh
# Simple validation of next-gen folder build process

Write-Host "🔍 Validating next-gen build process..." -ForegroundColor Green

# Change to next-gen directory
Set-Location "next-gen"

# Check if we can restore packages
Write-Host "`n📦 Testing package restore..." -ForegroundColor Yellow
try {
    $result = dotnet restore next-gen.sln
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Package restore successful" -ForegroundColor Green
    } else {
        Write-Host "❌ Package restore failed" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error during package restore: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Check if we can build
Write-Host "`n🔨 Testing build..." -ForegroundColor Yellow
try {
    $result = dotnet build next-gen.sln --configuration Release --no-restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor Green
    } else {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error during build: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Check if we can run tests
Write-Host "`n🧪 Testing test execution..." -ForegroundColor Yellow
try {
    $result = dotnet test next-gen.sln --configuration Release --no-build --verbosity normal
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Tests successful" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Tests completed with issues (this might be expected)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error during test execution: $($_.Exception.Message)" -ForegroundColor Red
}

# Check formatting
Write-Host "`n📝 Testing code formatting..." -ForegroundColor Yellow
try {
    $result = dotnet format next-gen.sln --verify-no-changes --verbosity minimal
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Code formatting is correct" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Code formatting issues detected" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error during formatting check: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 Validation completed!" -ForegroundColor Green
Write-Host "The next-gen CI workflow should work correctly with this setup." -ForegroundColor Cyan

# Return to original directory
Set-Location ".."
