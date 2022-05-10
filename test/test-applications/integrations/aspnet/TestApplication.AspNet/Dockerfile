# escape=`

ARG windowscontainer_version=ltsc2022
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-${windowscontainer_version}
ARG configuration=Debug
ARG platform=x64
ENV COR_ENABLE_PROFILING=1 `
    COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318} `
    COR_PROFILER_PATH=C:\opentelemetry\win-${platform}\OpenTelemetry.AutoInstrumentation.Native.dll `
    OTEL_DOTNET_AUTO_HOME=C:\opentelemetry\ `
    OTEL_DOTNET_AUTO_INTEGRATIONS_FILE=C:\opentelemetry\integrations.json `
    OTEL_TRACES_EXPORTER=zipkin `
    OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES=TestApplication.* `
    OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS=HttpClient,AspNet `
    OTEL_SERVICE_NAME=TestApplication.AspNet `
    OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION=1 `
    OTEL_DOTNET_AUTO_DEBUG=1 `
    OTEL_DOTNET_AUTO_LOG_DIRECTORY=C:\inetpub\wwwroot\logs `
    OTEL_DOTNET_AUTO_INCLUDE_PROCESSES=w3wp.exe
WORKDIR C:\inetpub\wwwroot
COPY bin/${configuration}/app.publish .
