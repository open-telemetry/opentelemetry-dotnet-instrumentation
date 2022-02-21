# Troubleshooting

Make sure that you are not hitting one of following issues:

## Handling of assembly version conflicts

OpenTelemetry SDK NuGet package are deployed with the OpenTelemetry .NET Instrumentation.
To handle conflicts in assemblies referenced by "source instrumentations", update the project 
references to ensure that they are on the same versions as the ones used by the instrumentation.

Previous workarounds only work at build time. When a rebuild is not possible, use one of the
following suggestions to force the application to use the assembly versions shipped with the 
instrumentation.

For .NET Framework applications, the workaround is to use binding redirects. For .NET Core
[Framework-dependent deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/deploy-with-vs?tabs=vs156#framework-dependent-deployment)
applications,
[Additional-deps](https://github.com/dotnet/runtime/blob/main/docs/design/features/additional-deps.md),
and [runtime package store](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)
from OpenTelemetry .NET, use the auto-instrumentation installation path.
For other .NET Core deployment models, edit the `deps.json` file.

### .NET Framework binding redirects

The [samples/BindingRedirect](./../samples/BindingRedirect/) app shows how
to use the `app.config` file to solve version conflicts.
The sample can only run successfully under the instrumentation, as the
binding redirect makes the application dependent on a version of `System.Diagnostics.DiagnosticSource`
that is not available at build time.

### .NET Core dependency file

To fix assembly version conflicts in .NET Core, edit the default `<application>.deps.json` file 
generated at build for .NET Core applications. Build a .NET Core app with package
references using the required version, and use the respective `deps.json` file to see what changes
are needed.

To test your edits to the `deps.json` file, add a reference to the required version of the OpenTelemetry
package to the [samples/CoreAppOldReference](./../samples/CoreAppOldReference/) sample and rebuild the
application. Save the generated `deps.json` file, remove the package reference, and rebuild the
sample app. Compare the files to explore the changes.

## No proper relationship between spans

On .NET Framework, strong name signing can force multiple versions of the same 
assembly being loaded on the same process. This causes a separate hierarchy of 
Activity objects. If you are referencing packages in your application that use a 
version of the `System.Diagnostics.DiagnosticSource` different than the `OpenTelemetry.Api` 
version used by the OpenTelemetry .NET Auto-Instrumentation (`6.0.0`) reference the `System.Diagnostics.DiagnosticSource` package in the correct version in your application.
This causes automatic binding redirection to solve the issue.

If automatic binding redirection is [disabled](https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/how-to-enable-and-disable-automatic-binding-redirection)
you can also manually add binding redirection to the [`App.config`](../samples/BindingRedirect/App.config) file.

## High CPU usage

Make sure that you have not enabled the auto-instrumentation globally
by setting the environment variables at system or user scope.

If the system or user scope is intended, use the `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`
and `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES` environment variables to include or exclude 
applications from the auto-instrumentation.

## Collect debug logs

Detailed debug logs can help you troubleshoot instrumentation issues, and can be
attached to issues in this project to facilitate investigation.

To get the detailed logs from the OpenTelemetry AutoInstrumentation for .NET, set 
the `OTEL_DOTNET_AUTO_DEBUG` environment variable to `true` before the 
instrumented process starts.

By default, the library writes the log files under predefined locations. If needed, 
change the default location by updating the `OTEL_DOTNET_AUTO_LOG_DIRECTORY` 
environment variable.

After obtaining the logs, remember to remove the `OTEL_DOTNET_AUTO_DEBUG` 
environment variable to avoid unnecessary overhead.
