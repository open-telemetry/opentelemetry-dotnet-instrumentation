# Next-Gen CI Testing Guide

This document explains how to test the new `next-gen-ci` workflow locally before pushing changes.

## Overview

The `next-gen-ci` workflow is designed specifically for the `out-of-process-collection` branch and only runs when changes are made to files within the `next-gen/` folder.

## Features

- **Smart triggering**: Only runs on `out-of-process-collection` branch when `next-gen/**` files change
- **Multi-platform testing**: Windows, Linux, macOS, and ARM64
- **Comprehensive checks**: Build, test, code quality, and security scanning
- **Proper isolation**: All operations scoped to `next-gen` folder
- **No conflicts**: Won't interfere with main branch CI when syncing changes
- **Standard SDK versions**: Uses repository-standard .NET 9.0.303

## Local Testing with `act`

### Prerequisites

1. **Install act**: 
   ```powershell
   winget install nektos.act
   ```

2. **Docker**: Required for running containers
   ```powershell
   docker --version
   ```

### Quick Validation Scripts

Two PowerShell scripts are provided for testing:

#### 1. Basic Build Validation
```powershell
.\validate-next-gen.ps1
```
This script tests the actual .NET build process in the `next-gen` folder:
- Package restore
- Solution build  
- Test execution
- Code formatting checks

#### 2. Workflow Testing
```powershell
.\test-next-gen-ci.ps1
```
This script validates the GitHub Actions workflow:
- Workflow syntax validation
- Dry-run of all jobs
- Confirms workflow structure

### Manual Testing with `act`

#### Test All Jobs (Dry Run)
```bash
# Test workflow syntax
act -W .github/workflows/next-gen-ci.yml --list

# Test individual jobs (dry run)
act -W .github/workflows/next-gen-ci.yml -j build-and-test -n
act -W .github/workflows/next-gen-ci.yml -j code-quality -n
act -W .github/workflows/next-gen-ci.yml -j security-scan -n
act -W .github/workflows/next-gen-ci.yml -j summary -n
```

#### Run Jobs for Real
```bash
# Run security scan (fastest)
act -W .github/workflows/next-gen-ci.yml -j security-scan

# Run code quality checks
act -W .github/workflows/next-gen-ci.yml -j code-quality

# Run full build and test (slowest, but most comprehensive)
act -W .github/workflows/next-gen-ci.yml -j build-and-test -P ubuntu-22.04=catthehacker/ubuntu:act-22.04
```

### Expected Results

#### ✅ Successful Validation
- All projects build without errors
- Tests pass (may have some warnings)
- Code formatting issues are reported as warnings (can be fixed)
- Security scan completes without vulnerabilities

#### ❌ Common Issues
- **Build failures**: Check if dependencies are restored
- **Test failures**: Review test output in generated artifacts
- **Format issues**: Run `dotnet format next-gen.sln` to fix
- **Security issues**: Review and update vulnerable packages

## Workflow Jobs

### 1. `build-and-test`
- **Purpose**: Build solution and run tests on multiple platforms
- **Platforms**: Windows 2022, Ubuntu 22.04, macOS 13, Ubuntu ARM64
- **Artifacts**: Test results for each platform

### 2. `code-quality`
- **Purpose**: Check code formatting and build with warnings as errors
- **Platform**: Ubuntu 22.04
- **Checks**: `dotnet format` and warning-free build

### 3. `security-scan`
- **Purpose**: Scan for vulnerable NuGet packages
- **Platform**: Ubuntu 22.04
- **Artifacts**: Vulnerability report (if any found)

### 4. `summary`
- **Purpose**: Aggregate results from all other jobs
- **Dependency**: Runs after all other jobs complete
- **Behavior**: Fails if any dependent job fails

## Tips for Development

1. **Test locally first**: Use the validation scripts before pushing
2. **Fix formatting**: Run `dotnet format next-gen.sln` to resolve style issues
3. **Check security**: Review any reported vulnerabilities
4. **Platform-specific issues**: Use `act` to test on Linux containers if developing on Windows

## Integration with Main Branch

This workflow is designed to:
- **Not conflict** with the main branch CI when syncing changes
- **Only run** when `next-gen/` files are modified
- **Use separate** artifact names to avoid collisions
- **Provide** clear status checks for the `out-of-process-collection` branch

The main CI workflow remains unchanged, preventing merge conflicts during branch synchronization.
