# escape=`

FROM mcr.microsoft.com/dotnet/framework/runtime:4.8-windowsservercore-ltsc2022@sha256:9008660d880fc07ce0fbe2e95cfba8f30a746d9e5ea887dcff1ac3bf0a75e671
ARG configuration=Debug
ARG platform=x64
ARG libraryVersion=10.0.2
WORKDIR /opentelemetry
COPY bin/tracer.zip .
COPY bin/OpenTelemetry.DotNet.Auto.psm1 .
ENV OTEL_DOTNET_AUTO_INSTALL_DIR=C:\opentelemetry `
    OTEL_DOTNET_AUTO_LOG_DIRECTORY=C:\app\logs `
    OTEL_LOG_LEVEL=debug
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
RUN Import-Module .\OpenTelemetry.DotNet.Auto.psm1 -Verbose; `
    Install-OpenTelemetryCore -LocalPath .\tracer.zip -RegisterAssembliesInGAC $false

WORKDIR /app
COPY bin/${libraryVersion}/${platform}/${configuration}/net462/ .
RUN New-Item -ItemType Directory -Path C:\app\logs -Force | Out-Null

ENTRYPOINT ["powershell", "-Command", "Import-Module C:\\opentelemetry\\OpenTelemetry.DotNet.Auto.psm1; Register-OpenTelemetryForCurrentSession -OTelServiceName 'TestApplication.AssemblyRedirection'; & 'C:\\app\\TestApplication.AssemblyRedirection.exe' $env:EXPECTED_ASSEMBLY_NAME $env:EXPECTED_ASSEMBLY_VERSION $env:EXPECTED_ASSEMBLY_FILE_VERSION; exit $LASTEXITCODE"]
