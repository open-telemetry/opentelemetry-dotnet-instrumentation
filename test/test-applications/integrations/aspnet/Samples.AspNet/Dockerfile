FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
ARG configuration=debug
ENV COR_ENABLE_PROFILING=1 \
    COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318} \
    COR_PROFILER_PATH=/inetpub/wwwroot/bin/profiler-lib/OpenTelemetry.ClrProfiler.Native.dll \
    OTEL_DOTNET_TRACER_HOME=/inetpub/wwwroot/bin/profiler-lib/ \
    OTEL_INTEGRATIONS=/inetpub/wwwroot/bin/profiler-lib/integrations.json \
    OTEL_EXPORTER=zipkin \
    OTEL_ENV=integration_tests \
    OTEL_DOTNET_TRACER_ADDITIONAL_SOURCES=Samples.* \
    OTEL_DOTNET_TRACER_INSTRUMENTATIONS=HttpClient,AspNet \
    OTEL_SERVICE=Samples.AspNet \
    OTEL_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION=1 \
    OTEL_TRACE_DEBUG=1 \
    OTEL_TRACE_LOG_DIRECTORY=/inetpub/wwwroot/logs \
    OTEL_PROFILER_PROCESSES=w3wp.exe
WORKDIR /inetpub/wwwroot
COPY bin/${configuration}/app.publish .
