# Instrument an ASP.NET application deployed on IIS

## Instrument an ASP.NET 4.x application

You can instrumental all ASP.NET 4.x application deployed to IIS
by setting the required environment variables for
`W3SVC` and `WAS` Windows Services as described in [windows-service-instrumentation.md](windows-service-instrumentation.md).

Unfortunately it is not possible to set distinct environment variables
values for the instrumented ASP.NET application.
Therefore all applications will share
the same configuration (e.g. `OTEL_SERVICE_NAME`).

## Instrument an ASP.NET Core application

Use the [`environmentVariable`](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config#set-environment-variables)
elements inside the `<aspNetCore>` block of your `web.config` file
to set environment variables.
