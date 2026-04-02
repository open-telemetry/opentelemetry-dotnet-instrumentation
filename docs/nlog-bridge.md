# `NLog` [logs bridge](https://opentelemetry.io/docs/specs/otel/glossary/#log-appender--bridge)

> [!IMPORTANT]
> NLog bridge is an experimental feature.

The `NLog` logs bridge is disabled by default. In order to enable it,
set `OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE` to `true`.

Bridge is supported for `NLOG` in versions >= 5.0.0 && < 7.0.0

If `NLOG` is used as a [logging provider](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-providers),
`NLOG` bridge should not be enabled, in order to reduce possibility of
duplicated logs export.

## `NLog` logging events conversion

`NLog`'s `ILoggingEvent`s are converted to OpenTelemetry log records in
a following way:

- `TimeStamp` is set as a `Timestamp`
- `Level.Name` is set as a `SeverityText`
- `FormattedMessage` is set as a `Body` if it is available
- Otherwise, `Message` is set as a `Body`
- `LoggerName` is set as an `InstrumentationScope.Name`
- `GetProperties()`, apart from builtin properties prefixed with `nlog:`, `NLog.`,
  are added as attributes
- `Exception` is used to populate the following properties: `exception.type`,
  `exception.message`, `exception.stacktrace`
- `Level.Value` is mapped to `SeverityNumber` as outlined in the next section

### `NLog` level severity mapping

`NLog` levels are mapped to OpenTelemetry severity types according to
 following rules based on their numerical values.

Levels with numerical values of:

- Equal to `LogLevel.Fatal` is mapped to `LogRecordSeverity.Fatal`
- Equal to `LogLevel.Error` is mapped to `LogRecordSeverity.Error`
- Equal to `LogLevel.Warn` is mapped to `LogRecordSeverity.Warn`
- Equal to `LogLevel.Info` is mapped to `LogRecordSeverity.Info`
- Equal to `LogLevel.Debug` is mapped to `LogRecordSeverity.Debug`
- Equal to `LogLevel.Trace` is mapped to `LogRecordSeverity.Trace`
- Equal to `LogLevel.Off` is mapped to `LogRecordSeverity.Trace`
- Any other is mapped to `LogRecordSeverity.Info`.
