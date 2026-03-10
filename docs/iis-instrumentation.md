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
> `Register-OpenTelemetryForIIS` performs IIS restart by default.
> Use `-NoReset` to skip the restart.

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

#### Disable Instrumentation per Application Pool (.NET Framework only)

You can use PowerShell module to quickly enable and disable instrumentation on
specific application pool.

```powershell
# Import the module
Import-Module "OpenTelemetry.DotNet.Auto.psm1"

# NOTE! Application pool name is case sensitive.
# It is warning only if a wrong application pool name is used.

# Adds COR_ENABLE_PROFILING=0 environment variable to MyAppPool config
Disable-OpenTelemetryForIISAppPool -AppPoolName MyAppPool

# Removes COR_ENABLE_PROFILING=0 environment variable from MyAppPool config
Enable-OpenTelemetryForIISAppPool -AppPoolName MyAppPool

# Restart Application Pool
Restart-WebAppPool -Name "MyAppPool"
```

> [!NOTE]
> The application pool environment variable takes precedence over
> global IIS registration.

You can also use IIS UI to configure and verify specific environment variables per
application pool.

1. Open Internet Information Service (IIS) Manager.
1. Select the server from the left.
1. Open 'Configuration Editor' from the Management section.
1. Open section 'system.applicationHost/applicationPools'
1. Press '...' in the first entry of the table (Collection).
1. Select row with your application pool name.
1. At the 'Properties' section, select 'environmentVariables' and press '...'.
1. Add or Remove environment variables.
1. Close all external windows and press 'Apply' in the main
   'Configuration Editor' view.
1. Restart your application.
