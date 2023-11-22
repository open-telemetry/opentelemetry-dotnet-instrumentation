# Instrument an ASP.NET application deployed on IIS

## Setup

Use the `OpenTelemetry.DotNet.Auto.psm1` PowerShell module
to set up automatic instrumentation for IIS:

```powershell
# Import the module
Import-Module "OpenTelemetry.DotNet.Auto.psm1"

# Install core files
Install-OpenTelemetryCore

# Setup IIS instrumentation
Register-OpenTelemetryForIIS
```

If you are instrumenting an ASP.NET Core application, you must also configure
the application's application pool with `.NET CLR Version` set to `No Managed Code`.
If this is not configured correctly, no telemetry data will be generated and
the debug-level tracer logs will show that no ReJIT's (bytecode rewriting) have
occurred. See this [issue](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2934#issuecomment-1746669737)
for further details.

> [!WARNING]
> `Register-OpenTelemetryForIIS` performs IIS restart.

## Configuration

> [!NOTE]
> Remember to restart IIS after making configuration changes.
> You can do it by executing `iisreset.exe`.

For ASP.NET application you can configure the most common `OTEL_` settings
(like `OTEL_SERVICE_NAME`) via `appSettings` in `Web.config`.

If a service name is not explicitly configured, one will be generated for you.
If the application is hosted on IIS in .NET Framework this will use
`SiteName\VirtualDirectoryPath` ex: `MySite\MyApp`

For ASP.NET Core application you can use
the [`<environmentVariable>`](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config#set-environment-variables)
elements inside the `<aspNetCore>` block of your `Web.config` file
to set configuration via environment variables.

### Advanced configuration

You can add the [`<environmentVariables>`](https://docs.microsoft.com/en-us/iis/configuration/system.applicationhost/applicationpools/add/environmentvariables/)
in `applicationHost.config`
to set environment variables for given application pools.

> For IIS versions older than 10.0, you can consider creating a distinct user,
  set its environment variables
  and use it as the application pool user.

Consider setting common environment variables,
for all applications deployed to IIS
by setting the environment variables for
`W3SVC` and `WAS` Windows Services as described in [windows-service-instrumentation.md](windows-service-instrumentation.md).
