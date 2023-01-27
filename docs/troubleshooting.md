# Troubleshooting

## dotnet is crashing

Instrumenting `dotnet` often results in a crash.
See [(#1744)](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/1744).

Therefore, we advise executing `dotnet build` before instrumenting the terminal session
or calling it in a seperate terminal session.

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

For .NET Framework applications the assembly references are, by default, updated
during runtime to the versions used by the automatic instrumentation.
This behavior can be controlled via the [`OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docs/config.md#additional-settings)
setting.

For the automatic redirection above to work there are two specific scenarios that
require the assemblies used to instrument .NET Framework
applications, the ones under the `netfx` folder of the installation directory,
to be also installed into the Global Assembly Cache (GAC):

1. [__Monkey patch instrumentation__](https://en.wikipedia.org/wiki/Monkey_patch#:~:text=Monkey%20patching%20is%20a%20technique,Python%2C%20Groovy%2C%20etc.)
of assemblies loaded as domain-neutral.
2. Assembly redirection for strong-named applications if the app also ships
different versions of some assemblies also shipped in the `netfx` folder.

If you are having problems in one of the scenarios above run again the
`Install-OpenTelemetryCore` command from the
[PowerShell installation module](../OpenTelemetry.DotNet.Auto.psm1)
to ensure that the required GAC installations are updated.

For more information about the GAC usage by the automatic instrumentation,
see [here](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/1906#issuecomment-1376292814).

## High CPU usage

Make sure that you have not enabled the automatic instrumentation globally
by setting the environment variables at system or user scope.

If the system or user scope is intended, use the `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`
environment variables to exclude applications from the automatic instrumentation.

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
