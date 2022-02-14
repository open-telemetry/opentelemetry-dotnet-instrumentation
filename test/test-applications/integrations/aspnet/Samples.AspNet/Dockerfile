FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
ARG configuration=debug
ARG platform=x64
ENV COR_ENABLE_PROFILING=1 \
    COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318} \
    COR_PROFILER_PATH=C:\\opentelemetry\\win-${platform}\\OpenTelemetry.ClrProfiler.Native.dll \
    OTEL_DOTNET_AUTO_HOME=C:\\opentelemetry\\ \
    OTEL_INTEGRATIONS=C:\\opentelemetry\\integrations.json \
    OTEL_TRACES_EXPORTER=zipkin \
    OTEL_DOTNET_TRACER_ADDITIONAL_SOURCES=Samples.* \
    OTEL_DOTNET_TRACER_INSTRUMENTATIONS=HttpClient,AspNet \
    OTEL_SERVICE_NAME=Samples.AspNet \
    OTEL_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION=1 \
    OTEL_TRACE_DEBUG=1 \
    OTEL_TRACE_LOG_DIRECTORY=C:\\inetpub\\wwwroot\\logs \
    OTEL_PROFILER_PROCESSES=w3wp.exe
WORKDIR C:\\inetpub\\wwwroot
COPY bin/${configuration}/app.publish .
