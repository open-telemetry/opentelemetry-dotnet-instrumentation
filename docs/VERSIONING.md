# Versioning and Stability

As per OpenTelemetry specification this repository uses [Semantic Versioning 2.0.0](https://semver.org/).
This means that all artifacts have a version of the format `MAJOR.MINOR.PATCH` or `MAJOR.MINOR.PATCH-<pre-release-id>`. For detailed information about version and stability for OpenTelemetry see
[Versioning and stability for OpenTelemetry clients](https://github.com/open-telemetry/opentelemetry-specification/blob/bccdb63e3da14d7eb3a4f3090e270126373069ba/specification/versioning-and-stability.md#versioning-and-stability-for-opentelemetry-clients).

In this repository `MAJOR` and `MINOR` version numbers are going to match the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry#opentelemetry-net-sdk)
currently shipped with the artifacts produced by CI.

As auto-instrumentation will inject assemblies during application runtime
conflicts between assemblies are possible and may demand the workarounds
listed at [Handling of assembly version conflicts](./troubleshooting.md/#handling-of-assembly-version-conflicts).

<!-- 
TODO:

Automate the generation of the dependencies table to be linked here.
It should include the package references from:

src\OpenTelemetry.AutoInstrumentation\OpenTelemetry.AutoInstrumentation.csproj
src\OpenTelemetry.AutoInstrumentation.AdditionalDeps\Directory.Build.props

See https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/454
-->
