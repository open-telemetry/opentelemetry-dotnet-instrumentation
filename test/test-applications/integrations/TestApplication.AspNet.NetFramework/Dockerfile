# escape=`

ARG windowscontainer_version=ltsc2022
ARG image_digest=sha256:4ad6f775ab425cf19af5c344750e3e259adce5a83f667ab1c9c2106e3e543cb7
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-${windowscontainer_version}@${image_digest}
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
