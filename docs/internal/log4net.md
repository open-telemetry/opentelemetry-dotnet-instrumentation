# `log4net` instrumentation

> [!IMPORTANT]
> log4net bridge and trace context injection are experimental features.
> Both instrumentations can be disabled by setting `OTEL_DOTNET_AUTO_LOGS_LOG4NET_INSTRUMENTATION_ENABLED` to `false`.

Both bridge and trace context injection are supported for `log4net` in versions >= 2.0.13 && < 4.0.0

## `log4net` [logs bridge](https://opentelemetry.io/docs/specs/otel/glossary/#log-appender--bridge)

The `log4net` logs bridge is disabled by default. In order to enable it, set `OTEL_DOTNET_AUTO_LOGS_ENABLE_LOG4NET_BRIDGE` to `true`.
When `log4net` logs bridge is enabled, and `log4net` is configured with at least 1 appender, application logs are exported in OTLP 
format by default to the local instance of OpenTelemetry Collector, in addition to being written into their currently configured destination (e.g. a file).

### `log4net` logging events conversion

`log4net`'s `LoggingEvent`s are converted to OpenTelemetry log records in a following way:

- `TimeStampUtc` is set as a `Timestamp`
- `Level.Name` is set as a `SeverityText`
- If formatted strings were used for logging (e.g. by using `InfoFormat` or similar), format string is set as a `Body`
- Otherwise, `RenderedMessage` is set as a `Body`
- If formatted strings were used for logging, format arguments are added as attributes, with indexes as their names
- If formatted strings were used for logging, and `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE` is set, rendered message
is added as `log4net.rendered_message` attribute
- `LoggerName` is set as an `InstrumentationScope.Name`
- `Properties`, apart from builtin properties prefixed with `log4net:`, are added as attributes
- `Exception` is used to populate the following properties: `exception.type`,`exception.message`,`exception.stacktrace`
- `Level.Value` is mapped to `SeverityNumber` as outlined in the next section

#### `log4net` level severity mapping

`log4net` levels are mapped to OpenTelemetry severity types according to the following rules based on their numerical values.

Levels with numerical values of:

- Equal to `Level.Fatal` or higher are mapped to `LogRecordSeverity.Fatal`
- Higher than or equal to `Level.Error` but lower than `Level.Fatal` are mapped to `LogRecordSeverity.Error`
- Higher than or equal to `Level.Warn` but lower than `Level.Error` are mapped to `LogRecordSeverity.Warn`
- Higher than or equal to `Level.Info` but lower than `Level.Warn` are mapped to `LogRecordSeverity.Info`
- Higher than or equal to `Level.Debug` but lower than `Level.Info` are mapped to `LogRecordSeverity.Debug`
- Lower than `Level.Debug` are mapped to `LogRecordSeverity.Trace`

## `log4net` trace context injection

Following properties are added by default to the collection of logging event's properties:

- `trace_id`
- `span_id`
- `trace_flags`

This allows for trace context to be logged into currently configured log destination, e.g. a file.
In order to use them, pattern needs to be updated.

## Known limitations of `log4net` bridge

In order for the bridge to be added, at least 1 other appender has to be configured.
Bridge should not be used when appenders are configured for both root and component loggers.
Enabling a bridge in such scenario would result in bridge being appended to both appender collections,
and logs duplication. 


