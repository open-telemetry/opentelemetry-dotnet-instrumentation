FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
ARG TRACER_VERSION=1.26.1
RUN mkdir -p /opt/datadog
RUN mkdir -p /var/log/opentelemetry/dotnet
RUN curl -L https://github.com/DataDog/dd-trace-dotnet/releases/download/v$TRACER_VERSION/datadog-dotnet-apm-$TRACER_VERSION.tar.gz | tar xzf - -C /opt/datadog

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
ENV CORECLR_PROFILER_PATH=/opt/otel-dotnet-autoinstrumentation/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so
ENV OTEL_DOTNET_TRACER_HOME=/opt/otel-dotnet-autoinstrumentation
ENV OTEL_INTEGRATIONS=/opt/otel-dotnet-autoinstrumentation/integrations.json
ENV OTEL_TRACE_ENABLED=true

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR "/src"
COPY ["reproductions/AutomapperTest/AutomapperTest.csproj", "/src/reproductions/AutomapperTest/"]
COPY ["src/Datadog.Trace.ClrProfiler.Managed/Datadog.Trace.ClrProfiler.Managed.csproj", "/src/src/Datadog.Trace.ClrProfiler.Managed/"]
COPY ["src/Datadog.Trace/Datadog.Trace.csproj", "/src/src/Datadog.Trace/"]
RUN dotnet restore "/src/reproductions/AutomapperTest/AutomapperTest.csproj"
COPY . .
WORKDIR "/src/reproductions/AutomapperTest"
RUN dotnet build "AutomapperTest.csproj" -c Release -o /app

FROM build as publish
RUN dotnet publish "AutomapperTest.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AutomapperTest.dll"]