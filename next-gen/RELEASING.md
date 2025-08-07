# Releasing OpenTelemetry.OutOfProcess.Forwarder Packages

This document describes how to release the OpenTelemetry.OutOfProcess.Forwarder packages.

## Overview

The OpenTelemetry.OutOfProcess.Forwarder consists of two NuGet packages:
- **OpenTelemetry.OutOfProcess.Forwarder** - Core forwarder functionality
- **OpenTelemetry.OutOfProcess.Forwarder.Configuration** - Configuration extensions

## Release Process

### 1. Prepare for Release

1. Ensure all changes are merged to the `out-of-process-collection` branch
2. Update version numbers and changelog if needed
3. Verify all tests are passing

### 2. Build Release Packages

1. Navigate to the repository's **Actions** tab
2. Select the **release-nextgen-forwarder-packages** workflow
3. Click **Run workflow**
4. Fill in the required parameters:
   - **Version**: Enter the release version (e.g., `1.0.0`, `1.0.0-alpha.1`, `1.0.0-beta.1`)
   - Ensure you're running from the `out-of-process-collection` branch
5. Click **Run workflow**

### 3. Verify Build

1. Wait for the workflow to complete successfully
2. Check the workflow summary for:
   - ✅ All tests passed
   - ✅ Packages were built and validated
   - ✅ Package sizes are reasonable
   - ✅ Artifact was uploaded

### 4. Download and Publish Packages

1. From the completed workflow run, download the `opentelemetry-nextgen-forwarder-packages` artifact
2. Extract the `.nupkg` files from the downloaded zip
3. Verify the package contents if needed:
   ```bash
   dotnet validate package local OpenTelemetry.OutOfProcess.Forwarder.{version}.nupkg
   dotnet validate package local OpenTelemetry.OutOfProcess.Forwarder.Configuration.{version}.nupkg
   ```

4. Publish to NuGet.org using your API key:
   ```bash
   dotnet nuget push OpenTelemetry.OutOfProcess.Forwarder.{version}.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   dotnet nuget push OpenTelemetry.OutOfProcess.Forwarder.Configuration.{version}.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   ```

### 5. Verify Publication

1. Check that packages appear on [nuget.org](https://www.nuget.org/packages?q=OpenTelemetry.OutOfProcess.Forwarder)
2. Verify package metadata and dependencies are correct
3. Test installation in a sample project:
   ```bash
   dotnet add package OpenTelemetry.OutOfProcess.Forwarder --version {version}
   dotnet add package OpenTelemetry.OutOfProcess.Forwarder.Configuration --version {version}
   ```

## Versioning Guidelines

- **Stable releases**: Use semantic versioning (e.g., `1.0.0`, `1.1.0`, `2.0.0`)
- **Preview releases**: Use preview suffixes (e.g., `1.0.0-alpha.1`, `1.0.0-beta.1`, `1.0.0-rc.1`)
- **Development builds**: Use preview suffixes with build numbers (e.g., `1.0.0-alpha.1.123`)

## Troubleshooting

### Build Failures
- Check that all project references are valid
- Ensure the `out-of-process-collection` branch is up to date
- Verify that tests are passing locally

### Package Validation Failures
- Review package dependencies and target frameworks
- Check for missing required metadata in `.csproj` files
- Ensure package content is properly included

### Publishing Failures
- Verify NuGet API key is valid and has correct permissions
- Check that package versions don't already exist (unless using `--skip-duplicate`)
- Ensure package names match exactly

## Related Documents

- [Main Repository Releasing Guide](../docs/releasing.md)
- [Package Configuration](src/OpenTelemetry.OutOfProcess.Forwarder/OpenTelemetry.OutOfProcess.Forwarder.csproj)
- [Workflow Definition](../.github/workflows/release-nextgen-forwarder-packages.yml)
