# Instrument an ASP.NET application deployed on IIS

## Instrument an ASP.NET 4.x application

You can instrumental all ASP.NET 4.x application deployed to IIS
by setting the required environment variables for
`W3SVC` and `WAS` Windows Services as described in [windows-service-instrumentation.md](windows-service-instrumentation.md).

> Unfortunately it is not possible to set distinct environment variables
  values for the instrumented ASP.NET application.
  Therefore all applications will share
  the same configuration (e.g. `OTEL_SERVICE_NAME`).

ASP.NET instrumentation on .NET Framework requires to install the
[`OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule/)
NuGet package in the instrumented project.
The following shows changes required to your `Web.config`:

```xml
<system.webServer>
  <modules>
    <add
      name="TelemetryHttpModule"
      type="OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule, OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule"
      preCondition="integratedMode,managedHandler" />
    </modules>
</system.webServer>
```

Make sure to enable the ASP.NET instrumentation by setting
`OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS=AspNet`.

## Instrument an ASP.NET Core application

Use the [`environmentVariable`](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config#set-environment-variables)
elements inside the `<aspNetCore>` block of your `Web.config` file
to set environment variables.

Make sure to enable the ASP.NET instrumentation by setting
`OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS=AspNet`.
