# Troubleshooting

## Handling of assembly version conflicts

OpenTelemetry .NET NuGet packages are deployed with
the OpenTelemetry .NET Automatic Instrumentation.
To handle conflicts in assemblies referenced by "source instrumentations",
update the project references to ensure that they are on the same versions
as the ones used by the instrumentation.

This workaround works only at build time. When a rebuild is not possible,
use one of the following suggestions to force the application to use
the assembly versions shipped with the instrumentation.

For .NET Framework applications, the workaround is to use binding redirects.
The [examples/BindingRedirect](./../examples/BindingRedirect/) app shows how
to use the `app.config` file to solve version conflicts.
The example can only run successfully under the instrumentation, as the
binding redirect makes the application dependent on a version of
`System.Diagnostics.DiagnosticSource` that is not available at build time.

For .NET Core [Framework-dependent deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/deploy-with-vs?tabs=vs156#framework-dependent-deployment)
applications, [Additional-deps](https://github.com/dotnet/runtime/blob/main/docs/design/features/additional-deps.md),
and [runtime package store](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)
from OpenTelemetry .NET, use the automatic instrumentation installation path.

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
