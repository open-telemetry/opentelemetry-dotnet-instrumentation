# escape=`

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022@sha256:84079c734ab5aec702409ef7967ec47af9468c56ff4046882239cabacda78097 AS integrated-base
ARG configuration=Debug
ARG platform=x64
WORKDIR /opentelemetry
COPY bin/tracer.zip .
COPY bin/OpenTelemetry.DotNet.Auto.psm1 .
ENV OTEL_DOTNET_AUTO_INSTALL_DIR=C:\opentelemetry
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
RUN Import-Module .\OpenTelemetry.DotNet.Auto.psm1 -Verbose; `
    Install-OpenTelemetryCore -LocalPath .\tracer.zip -RegisterAssembliesInGAC $false; `
    Register-OpenTelemetryForIIS;
ENV OTEL_DOTNET_AUTO_LOG_DIRECTORY=C:\inetpub\wwwroot\logs `
    OTEL_LOG_LEVEL=debug 
WORKDIR /inetpub/wwwroot
COPY bin/${configuration}/app.publish .

FROM integrated-base AS integrated-nogac
RUN Start-IISCommitDelay;`
    (Get-IISAppPool -Name DefaultAppPool).ProcessModel.LoadUserProfile = $true;`
    Stop-IISCommitDelay -Commit $True

FROM integrated-nogac AS classic-nogac
RUN Start-IISCommitDelay;`
    (Get-IISAppPool -Name DefaultAppPool).ManagedPipelineMode = 'Classic';`
    (Get-IISAppPool -Name DefaultAppPool).ProcessModel.LoadUserProfile = $true;`
    Stop-IISCommitDelay -Commit $True

FROM integrated-base AS integrated
WORKDIR /opentelemetry
RUN Import-Module .\OpenTelemetry.DotNet.Auto.psm1 -Verbose; `
    Register-AssembliesInGAC -InstallDir C:\inetpub\wwwroot\otel;
WORKDIR /inetpub/wwwroot

FROM integrated AS classic
RUN Start-IISCommitDelay;`
    (Get-IISAppPool -Name DefaultAppPool).ManagedPipelineMode = 'Classic';`
    Stop-IISCommitDelay -Commit $True