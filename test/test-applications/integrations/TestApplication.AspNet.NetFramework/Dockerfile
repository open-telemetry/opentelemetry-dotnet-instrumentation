# escape=`

ARG windowscontainer_version=ltsc2022
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-${windowscontainer_version}
ARG configuration=Debug
ARG platform=x64
WORKDIR C:\opentelemetry
COPY bin/tracer-home .
WORKDIR C:\init
COPY docker-init.ps1 .
RUN [ "powershell", "-c", ".\\docker-init.ps1" ]
ENV COR_ENABLE_PROFILING=1 `
    COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318} `
    COR_PROFILER_PATH=C:\opentelemetry\win-${platform}\OpenTelemetry.AutoInstrumentation.Native.dll `
    OTEL_DOTNET_AUTO_HOME=C:\opentelemetry\ `
    OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES=TestApplication.* `
    OTEL_LOG_LEVEL=debug `
    OTEL_DOTNET_AUTO_LOG_DIRECTORY=C:\inetpub\wwwroot\logs
WORKDIR C:\inetpub\wwwroot
COPY bin/${configuration}/app.publish .
