# Instrument an ASP.NET application deployed on IIS

## Set environment variables

You can add the [`<environmentVariables>`](https://docs.microsoft.com/en-us/iis/configuration/system.applicationhost/applicationpools/add/environmentvariables/)
in `applicationHost.config`
to set environment variables for given application pools.

> For IIS versions older than 10.0, you can consider creating a distinct user,
  set its environment variables
  and use it as the application pool user.

For ASP.NET Core application you can also use
the [`<environmentVariable>`](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config#set-environment-variables)
elements inside the `<aspNetCore>` block of your `Web.config` file
to set environment variables.

You can consider setting common environment variables,
such us `COR_PROFILER`,
for all application deployed to IIS
by setting the environment variables for
`W3SVC` and `WAS` Windows Services as described in [windows-service-instrumentation.md](windows-service-instrumentation.md).

## Add TelemetryHttpModule ASP.NET HTTP module

> This is NOT required for ASP.NET Core deployments.

This step is necessary only for ASP.NET (.NET Framework).

Add `OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule, OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule`
ASP.NET HTTP module to your application's `Web.config`.
You can add it in the following places:

```xml
  <system.web>
    <httpModules>
      <add name="TelemetryHttpModule" type="OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule, OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule" />
    </httpModules>
  </system.web>
```

```xml
  <system.webServer>
    <validation validateIntegratedModeConfiguration="false" />
    <modules>
      <remove name="TelemetryHttpModule" />
      <add name="TelemetryHttpModule" type="OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule, OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule" preCondition="managedHandler" />
    </modules>
  </system.webServer>
```

The ASP.NET HTTP module can be also set in `applicationHost.config`.
Here is an example where you can add the module
to set it for all ASP.NET application runing in Integrated Pipeline Mode:

```xml
  <location path="" overrideMode="Allow">
    <system.webServer>
      <modules>
        <add name="TelemetryHttpModule" type="OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule, OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule" preCondition="managedHandler" />
      </modules>
    </system.webServer>
  </location>
```

## Enable ASP.NET instrumentation

Make sure to enable the ASP.NET instrumentation by setting
`OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS=AspNet`.
