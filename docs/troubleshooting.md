# Troubleshooting

## Assembly version conflicts

OpenTelemetry .NET NuGet packages and its dependencies
are deployed with the OpenTelemetry .NET Automatic Instrumentation.

In case of assembly version conflicts you may get a `TargetInvocationException`.
For example:

```txt
Unhandled exception. System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.TypeInitializationException: The type initializer for 'OpenTelemetry.AutoInstrumentation.Loader.Startup' threw an exception.
 ---> System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.TypeLoadException: Could not load type 'OpenTelemetry.Exporter.OtlpExportProtocol' from assembly 'OpenTelemetry.Exporter.OpenTelemetryProtocol, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7bd6737fe5b67e3c'.
```

To handle dependency versions conflicts,
update the instrumented application's project references
to use the same versions.

When a rebuild is not possible,
for .NET Framework applications the workaround is to use binding redirects.
The [examples/BindingRedirect](./../examples/BindingRedirect/) app shows how
to use the `app.config` file to solve version conflicts.
The example can only run successfully under the instrumentation, as the
binding redirect makes the application dependent on a version of
`System.Diagnostics.DiagnosticSource` that is not available at build time.

## No proper relationship between spans

On .NET Framework, strong name signing can force the loading of multiple versions
of the same assembly on the same process. This causes a separate hierarchy of
Activity objects. If you are referencing packages in your application that use a
version of the `System.Diagnostics.DiagnosticSource` different than the `OpenTelemetry.Api`
version used by the OpenTelemetry .NET Automatic Instrumentation, reference
the correct version of the `System.Diagnostics.DiagnosticSource` package
in your application.
This causes automatic binding redirection to solve the issue.

If automatic binding redirection is [disabled](https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/how-to-enable-and-disable-automatic-binding-redirection)
you can also manually add binding redirection to the [`App.config`](../examples/BindingRedirect/App.config)
file.

## High CPU usage

Make sure that you have not enabled the automatic instrumentation globally
by setting the environment variables at system or user scope.

If the system or user scope is intended, use the `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`
and `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES` environment variables to include or exclude
applications from the automatic instrumentation.

## Collect debug logs

Detailed debug logs can help you troubleshoot instrumentation issues, and can be
attached to issues in this project to facilitate investigation.

To get the detailed logs from the OpenTelemetry .NET Automatic Instrumentation, set
the `OTEL_DOTNET_AUTO_DEBUG` environment variable to `true` before the
instrumented process starts.

By default, the library writes the log files under predefined locations. If needed,
change the default location by updating the `OTEL_DOTNET_AUTO_LOG_DIRECTORY`
environment variable.

After obtaining the logs, remove the `OTEL_DOTNET_AUTO_DEBUG`
environment variable to avoid unnecessary overhead.

## Nothing happens

It may occur that the .NET Profiler is unable to attach
and therefore no logs would be emitted.

The most common reason is that the instrumented application
has no permissions to load the OpenTelemetry .NET Automatic Instrumentation
assemblies.
