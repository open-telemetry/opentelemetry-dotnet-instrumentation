FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -f netcoreapp3.1 -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build /app/out .

# Set up Datadog APM
ARG TRACER_VERSION=1.24.0
RUN mkdir -p /var/log/datadog
RUN mkdir -p /opt/datadog
RUN curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb
RUN dpkg -i ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV OTEL_INTEGRATIONS=/opt/datadog/integrations.json
ENV OTEL_DOTNET_TRACER_HOME=/opt/datadog

CMD ["dotnet", "ConsoleApp.dll"]