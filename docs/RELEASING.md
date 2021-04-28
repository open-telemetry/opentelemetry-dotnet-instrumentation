# Releasing

## Bumping version

To change the version the following steps have to be executed:
* Update the version number in [TracerVersion.cs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/tools/Datadog.Core.Tools/TracerVersion.cs).
* Run the [PrepareRelease tool](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/tree/main/build/tools/PrepareRelease): `dotnet run versions integrations`.