# Releasing

## Pre-Release

### Bumping version

To change the version the following steps have to be executed:
* Update the version number in [TracerVersion.cs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/tools/Datadog.Core.Tools/TracerVersion.cs).
* Run the [PrepareRelease tool](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/tree/main/build/tools/PrepareRelease): `dotnet run msi versions integrations`.

## Release

To create GitHub release the git tag with correct version number has to be pushed (format is `v\d.\d.\d`, it must match the version prepared in Pre-Release steps). The release will be created automatically by the GitHub Actions workflow, but the realese notes have to be added manually.