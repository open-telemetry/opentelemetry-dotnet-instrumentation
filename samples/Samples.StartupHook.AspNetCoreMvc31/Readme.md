# ASP.NET Core Startup Hook Sample
This sample demonstrates how to inject the OpenTelemetry SDK into an ASP.NET Core app via the [DOTNET_STARTUP_HOOK](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md).

## Run ASP.NET Core app
You can build and run a ASP.NET Core app using the following instructions:

```console
docker build
dotnet run --launch-profile "Samples.StartupHook.AspNetCoreMvc31"
```
