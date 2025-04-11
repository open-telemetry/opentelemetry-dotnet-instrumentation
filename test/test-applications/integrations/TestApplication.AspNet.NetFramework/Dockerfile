# escape=`

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022@sha256:84079c734ab5aec702409ef7967ec47af9468c56ff4046882239cabacda78097 AS integrated
ARG configuration=Debug
ARG platform=x64
WORKDIR /opentelemetry
COPY bin/tracer.zip .
COPY bin/OpenTelemetry.DotNet.Auto.psm1 .
ENV OTEL_DOTNET_AUTO_INSTALL_DIR=C:\opentelemetry
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
RUN Set-ItemProperty -Path "HKLM:\\SOFTWARE\\Microsoft\\.NETFramework" -Name "LoaderOptimization" -Value 1
RUN Import-Module .\OpenTelemetry.DotNet.Auto.psm1 -Verbose; `
    Install-OpenTelemetryCore -LocalPath .\tracer.zip; `
    Register-OpenTelemetryForIIS;
ENV OTEL_DOTNET_AUTO_LOG_DIRECTORY=C:\inetpub\wwwroot\logs `
    OTEL_LOG_LEVEL=debug
WORKDIR /inetpub/wwwroot
COPY bin/${configuration}/app.publish .

FROM integrated AS classic
RUN Start-IISCommitDelay;`
    (Get-IISAppPool -Name DefaultAppPool).ManagedPipelineMode = 'Classic';`
    Stop-IISCommitDelay -Commit $True