# escape=`

FROM mcr.microsoft.com/dotnet/framework/wcf:4.8-windowsservercore-ltsc2022@sha256:5ae98afc5e31680c31662897ee453655f103f5923588991017bc561bc6312e08
ARG configuration=Debug
ARG platform=x64
WORKDIR /opentelemetry
COPY bin/tracer.zip .
COPY bin/OpenTelemetry.DotNet.Auto.psm1 .
ENV OTEL_DOTNET_AUTO_INSTALL_DIR=C:\opentelemetry
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
RUN Import-Module .\OpenTelemetry.DotNet.Auto.psm1 -Verbose; `
    Install-OpenTelemetryCore -LocalPath .\tracer.zip; `
    Register-OpenTelemetryForIIS;
ENV OTEL_DOTNET_AUTO_LOG_DIRECTORY=C:\inetpub\wwwroot\logs `
    OTEL_LOG_LEVEL=debug
WORKDIR /inetpub/wwwroot
COPY bin/${configuration}/app.publish .