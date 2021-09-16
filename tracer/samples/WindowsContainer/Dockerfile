#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:5.0-windowsservercore-ltsc2019 AS base
WORKDIR /app

ARG TRACER_VERSION=0.0.1
ENV OTEL_TRACER_VERSION=$TRACER_VERSION
ENV ASPNETCORE_URLS=http://*.80

ENV COR_ENABLE_PROFILING="1"
ENV COR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"

ENV CORECLR_ENABLE_PROFILING="1"
ENV CORECLR_PROFILER=$COR_PROFILER

# We recommend always using the latest release and regularly updating: https://github.com/DataDog/dd-trace-dotnet/releases/latest
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

RUN Write-Host "Downloading Datadog .NET Tracer v$env:OTEL_TRACER_VERSION" ;\
    (New-Object System.Net.WebClient).DownloadFile('https://github.com/DataDog/dd-trace-dotnet/releases/download/v' + $env:OTEL_TRACER_VERSION + '/datadog-dotnet-apm-' + $env:OTEL_TRACER_VERSION + '-x64.msi', 'datadog-apm.msi') ;\
    Write-Host 'Installing Datadog .NET Tracer' ;\
    Start-Process -Wait msiexec -ArgumentList '/i datadog-apm.msi /quiet /qn /norestart /log datadog-apm-msi-installer.log' ; \
    Write-Host 'Datadog .NET Tracer installed, removing installer file' ; \
	Remove-Item 'datadog-apm.msi' ;

FROM mcr.microsoft.com/dotnet/sdk:5.0-windowsservercore-ltsc2019 AS build
WORKDIR /src
COPY ["Dotnet.WindowsContainer.Example.csproj", "."]
RUN dotnet restore "./Dotnet.WindowsContainer.Example.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet publish "Dotnet.WindowsContainer.Example.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dotnet.WindowsContainer.Example.dll"]
