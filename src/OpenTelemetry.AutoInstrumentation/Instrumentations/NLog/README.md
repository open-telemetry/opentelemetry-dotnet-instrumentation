# NLog OpenTelemetry Auto-Instrumentation

This directory contains the NLog instrumentation for OpenTelemetry .NET Auto-Instrumentation. This instrumentation provides two approaches for bridging NLog logging events to OpenTelemetry: automatic zero-config injection and standard NLog Target configuration.

## Overview

The NLog instrumentation offers flexible integration through:
1. **Zero-Config Auto-Injection**: Automatically injects `OpenTelemetryTarget` into existing NLog configurations
2. **Standard NLog Target**: `OpenTelemetryTarget` can be configured like any NLog target via `nlog.config` or code
3. **Log Event Bridging**: Converting NLog log events to OpenTelemetry log records
4. **Structured Logging Support**: Leveraging NLog's layout abilities for enrichment
5. **Trace Context Integration**: Automatically including trace context in log records
6. **Custom Properties**: Forwarding custom properties while filtering internal NLog properties

## Architecture

### Zero-Config Path (Auto-Injection)
```
NLog Logger.Log() Call
    ↓
LoggerIntegration (CallTarget)
    ↓
NLogAutoInjector.EnsureConfigured()
    ↓
OpenTelemetryTarget → NLog Configuration
    ↓
OpenTelemetryNLogConverter
    ↓
OpenTelemetry LogRecord
    ↓
OTLP Exporters
```

### Standard NLog Target Path
```
NLog Configuration (nlog.config or code)
    ↓
OpenTelemetryTarget (TargetWithContext)
    ↓
OpenTelemetryNLogConverter
    ↓
OpenTelemetry LogRecord
    ↓
OTLP Exporters
```

## Components

### Core Components

#### Auto-Instrumentation Components
- **`ILoggingEvent.cs`**: Duck typing interface for NLog's LogEventInfo
- **`OpenTelemetryNLogConverter.cs`**: Internal converter that transforms NLog events to OpenTelemetry log records
- **`OpenTelemetryLogHelpers.cs`**: Helper for creating OpenTelemetry log records via expression trees
- **`NLogAutoInjector.cs`**: Handles programmatic injection of OpenTelemetryTarget into NLog configuration

#### Standard NLog Target
- **`OpenTelemetryTarget.cs`** (in `OpenTelemetry.AutoInstrumentation.NLogTarget` project): Standard NLog target extending `TargetWithContext`

### Integration

- **`LoggerIntegration.cs`**: CallTarget integration that intercepts `NLog.Logger.Log` to trigger auto-injection and GDC trace context

### Trace Context

- **`LogsTraceContextInjectionConstants.cs`**: Constants for trace context property names

## Configuration

### Auto-Injection (Zero-Config)

The NLog auto-injection is controlled by environment variables:

- `OTEL_DOTNET_AUTO_LOGS_ENABLED=true`: Enables logging instrumentation
- `OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE=true`: Enables the NLog bridge specifically
- `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE=true`: Includes formatted messages as attributes

### Standard NLog Target Configuration

#### Via nlog.config
```xml
<nlog>
  <extensions>
    <add assembly="OpenTelemetry.AutoInstrumentation.NLogTarget" />
  </extensions>
  
  <targets>
    <target xsi:type="OpenTelemetryTarget" 
            name="otlp"
            endpoint="http://localhost:4317"
            useHttp="false"
            includeFormattedMessage="true"
            includeEventProperties="true">
      <attribute name="service.name" layout="${var:ServiceName}" />
      <attribute name="deployment.environment" layout="${var:Environment}" />
    </target>
  </targets>
  
  <rules>
    <logger name="*" minlevel="Trace" writeTo="otlp" />
  </rules>
</nlog>
```

#### Via Code
```csharp
var config = new LoggingConfiguration();
var otlpTarget = new OpenTelemetryTarget
{
    Endpoint = "http://localhost:4317",
    UseHttp = false,
    IncludeFormattedMessage = true
};
config.AddTarget("otlp", otlpTarget);
config.AddRule(LogLevel.Trace, LogLevel.Fatal, otlpTarget);
LogManager.Configuration = config;
```

## Supported Versions

- **NLog**: 5.0.0+ (required for Layout&lt;T&gt; typed layout support and .NET build-trimming)
- **.NET Framework**: 4.6.2+
- **.NET**: 6.0+, 8.0, 9.0

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

- **Logger Caching**: OpenTelemetry loggers are cached (up to 100) to avoid recreation overhead
- **Lazy Initialization**: Components are initialized only when needed
- **Minimal Overhead**: The target is injected once during configuration loading

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
- Both auto-injection and manual target configuration paths

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
- Target injection success/failure
- Logger creation and caching
- Property filtering decisions

## Implementation Notes

- Uses reflection to access internal OpenTelemetry logging APIs (until public APIs are available)
- Builds expression trees dynamically for efficient log record creation
- Follows the same patterns as Log4Net instrumentation for consistency
- Designed to be thread-safe and performant in high-throughput scenarios 