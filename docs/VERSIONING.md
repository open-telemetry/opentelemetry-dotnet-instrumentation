# Versioning and Stability

As per the OpenTelemetry specification this repository uses [Semantic Versioning 2.0.0](https://semver.org/).
This means that the version of all artifacts is in the format `MAJOR.MINOR.PATCH` or `MAJOR.MINOR.PATCH-<pre-release-id>`. For detailed information about version and stability for OpenTelemetry see
[Versioning and stability for OpenTelemetry clients](https://github.com/open-telemetry/opentelemetry-specification/blob/bccdb63e3da14d7eb3a4f3090e270126373069ba/specification/versioning-and-stability.md#versioning-and-stability-for-opentelemetry-clients).

In this repository, `MAJOR` and `MINOR` version numbers match the version numbers of the
[OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry#opentelemetry-net-sdk) shipped with the artifacts produced by CI.

As the auto-instrumentation injects assemblies at application runtime,
conflicts between assemblies might happen and might require workarounds.
See [Handling of assembly version conflicts](./troubleshooting.md/#handling-of-assembly-version-conflicts) for more information.

<!-- 
TODO:

Automate the generation of the dependencies table to be linked here.
It should include the package references from:

src\OpenTelemetry.AutoInstrumentation\OpenTelemetry.AutoInstrumentation.csproj
src\OpenTelemetry.AutoInstrumentation.AdditionalDeps\Directory.Build.props

See https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/454
-->
