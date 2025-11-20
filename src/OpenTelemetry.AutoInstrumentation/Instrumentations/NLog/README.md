# NLog OpenTelemetry Auto-Instrumentation

This directory contains the NLog instrumentation for OpenTelemetry .NET Auto-Instrumentation. This instrumentation provides automatic bytecode interception for bridging NLog logging events to OpenTelemetry using duck typing.

## Overview

The NLog instrumentation offers automatic integration through:
1. **Bytecode Interception**: Automatically intercepts `NLog.Logger.Log` calls via bytecode instrumentation
2. **Duck Typing Integration**: Uses duck typing to avoid direct NLog assembly references
3. **Log Event Bridging**: Converting NLog log events to OpenTelemetry log records (when bridge is enabled)
4. **Structured Logging Support**: Leveraging NLog's layout abilities for enrichment
5. **Trace Context Injection**: Automatically injects trace context into NLog properties for all targets
6. **Custom Properties**: Forwarding custom properties while filtering internal NLog properties

**Note**: No NLog configuration changes are required. The instrumentation works exclusively through bytecode interception and relies on OpenTelemetry environment variables for configuration.

## Architecture

### Bytecode Interception Path
```
NLog Logger.Log() Call
    ↓
LoggerIntegration (CallTarget - Bytecode Interception)
    ├─ ALWAYS: Inject trace context into NLog properties
    │   (Available to ALL NLog targets: file, console, database, etc.)
    │
    └─ IF bridge enabled: Forward to OpenTelemetry
        ↓
        OpenTelemetryNLogConverter.WriteLogEvent()
        ↓
        OpenTelemetry LogRecord
        ↓
        OTLP Exporters
```

The instrumentation intercepts `NLog.Logger.Log` method calls at the bytecode level, allowing it to:
1. **Always inject trace context** into NLog's LogEventInfo properties (regardless of bridge status)
2. **Optionally forward logs** to OpenTelemetry when the bridge is enabled

## Components

### Core Components

#### Auto-Instrumentation Components
- **`ILoggingEvent.cs`**: Duck typing interface for NLog's LogEventInfo
- **`OpenTelemetryNLogConverter.cs`**: Internal converter that transforms NLog events to OpenTelemetry log records
- **`OpenTelemetryLogHelpers.cs`**: Helper for creating OpenTelemetry log records via expression trees

### Integration

- **`LoggerIntegration.cs`**: CallTarget integration that intercepts `NLog.Logger.Log` via bytecode instrumentation to capture log events

### Trace Context

- **`LogsTraceContextInjectionConstants.cs`**: Constants for trace context property names

## Configuration

The NLog instrumentation is configured entirely through OpenTelemetry environment variables. No programmatic configuration is supported to maintain assembly loading safety.

### Environment Variables

The NLog bridge is controlled by:

- `OTEL_DOTNET_AUTO_LOGS_ENABLED=true`: Enables logging instrumentation
- `OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE=true`: Enables the NLog bridge specifically

Standard OpenTelemetry environment variables configure the OTLP exporter:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
export OTEL_EXPORTER_OTLP_HEADERS="x-api-key=abc123"
export OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
export OTEL_RESOURCE_ATTRIBUTES="service.name=MyApp,service.version=1.0.0"
export OTEL_BSP_SCHEDULE_DELAY="5000"
export OTEL_BSP_MAX_QUEUE_SIZE="2048"
export OTEL_BSP_MAX_EXPORT_BATCH_SIZE="512"
```

### Behavior

The instrumentation automatically:
- **Injects trace context** into NLog properties (TraceId, SpanId, TraceFlags) for ALL NLog targets
- Uses formatted message if available, otherwise raw message (when bridge enabled)
- Includes event parameters when present (when bridge enabled)
- Captures trace context from `Activity.Current`
- Forwards custom properties while filtering internal NLog properties (when bridge enabled)

#### Trace Context Injection

Trace context is **always injected** into NLog's LogEventInfo properties, regardless of whether the OpenTelemetry bridge is enabled. This allows NLog's own targets (file, console, database, etc.) to access trace context using NLog's layout renderers:

```xml
<target xsi:type="Console" name="console"
        layout="${longdate} ${message} TraceId=${event-properties:TraceId} SpanId=${event-properties:SpanId}" />
```

The following properties are injected when an active `Activity` exists:
- `TraceId`: The W3C trace ID
- `SpanId`: The W3C span ID  
- `TraceFlags`: The W3C trace flags

#### OpenTelemetry Bridge

When `OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE=true`, log events are additionally forwarded to OpenTelemetry's logging infrastructure for export via OTLP or other configured exporters.

## Supported Versions

- **NLog**: 5.0.0+ (required for Layout&lt;T&gt; typed layout support and .NET build-trimming)
- **.NET Framework**: 4.6.2+
- **.NET**: 8.0, 9.0

## Level Mapping

NLog levels are mapped to OpenTelemetry log record severity levels:

| NLog Level | Ordinal | OpenTelemetry Severity | Value |
|------------|---------|------------------------|-------|
| Trace      | 0       | Trace                  | 1     |
| Debug      | 1       | Debug                  | 5     |
| Info       | 2       | Info                   | 9     |
| Warn       | 3       | Warn                   | 13    |
| Error      | 4       | Error                  | 17    |
| Fatal      | 5       | Fatal                  | 21    |
| Off        | 6       | Trace                  | 1     |

## Duck Typing

The instrumentation uses duck typing to interact with NLog without requiring direct references:

- **`ILoggingEvent`**: Maps to `NLog.LogEventInfo`
- **`LoggingLevel`**: Maps to `NLog.LogLevel`
- **`IMessageTemplateParameters`**: Maps to structured logging parameters

## Property Filtering

The following properties are filtered out when forwarding to OpenTelemetry:
- Properties starting with `NLog.`
- Properties starting with `nlog:`
- OpenTelemetry trace context properties (`SpanId`, `TraceId`, `TraceFlags`)

## Performance Considerations

- **Logger Caching**: OpenTelemetry loggers are cached to avoid recreation overhead
- **Lazy Initialization**: Components are initialized only when needed
- **Minimal Overhead**: Bytecode interception adds minimal overhead to logging calls

## Error Handling

- **Graceful Degradation**: If OpenTelemetry components fail to initialize, logging continues normally
- **Property Safety**: Property extraction is wrapped in try-catch to handle potential NLog configuration issues
- **Instrumentation Conflicts**: Automatically disables when other logging bridges are active

## Testing

Tests are located in `test/OpenTelemetry.AutoInstrumentation.Tests/NLogTests.cs` and cover:
- Level mapping verification
- Edge case handling (invalid levels, off level)
- Custom level support
- Range-based mapping logic

## Integration Testing

A complete test application is available at `test/test-applications/integrations/TestApplication.NLogBridge/` that demonstrates:
- Direct NLog usage
- Microsoft.Extensions.Logging integration via custom provider
- Structured logging scenarios
- Exception logging
- Custom properties
- Trace context propagation

## Troubleshooting

### Common Issues

1. **Bridge Not Working**
   - Verify `OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE=true`
   - Check that NLog version is supported
   - Ensure auto-instrumentation is properly loaded

2. **Missing Properties**
   - Check NLog configuration for property capture
   - Verify properties don't start with filtered prefixes

3. **Performance Impact**
   - Monitor logger cache efficiency
   - Consider adjusting cache size if many dynamic logger names are used

### Debug Information

Enable debug logging to see:
- Bytecode interception success/failure
- Logger creation and caching
- Property filtering decisions

## Implementation Notes

- Uses reflection to access internal OpenTelemetry logging APIs (until public APIs are available)
- Builds expression trees dynamically for efficient log record creation
- Follows the same patterns as Log4Net instrumentation for consistency
- Designed to be thread-safe and performant in high-throughput scenarios