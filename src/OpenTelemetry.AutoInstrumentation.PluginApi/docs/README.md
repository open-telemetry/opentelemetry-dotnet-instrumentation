# Using the OpenTelemetry.AutoInstrumentation.PluginApi NuGet package

Use this package to build plugins for OpenTelemetry .NET Automatic
Instrumentation. Every plugin must implement
`OpenTelemetry.AutoInstrumentation.PluginApi.IPlugin` and can opt into
additional extension points through the telemetry, OpAMP, selective sampling,
and continuous profiling interfaces in this package.

Use the same `OpenTelemetry.AutoInstrumentation.PluginApi` package version as
the OpenTelemetry .NET Automatic Instrumentation version that will load the
plugin.

See the
[plugins documentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docs/plugins.md)
for the full API and examples.
