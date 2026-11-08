# `Serilog` [logs bridge](https://opentelemetry.io/docs/specs/otel/glossary/#log-appender--bridge)

> [!IMPORTANT]
> Serilog bridge is an experimental feature.

The `Serilog` logs bridge is disabled by default. To enable it,
set `OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE` to `true`.

The bridge supports `Serilog` versions >= 2.0.0 && < 5.0.0.

The bridge exports the events that pass each logger's filters and
minimum level.

## Duplicated logs

If `Serilog` is added as one `ILogger` provider with
`ILoggingBuilder.AddSerilog()`, do not enable the bridge. Events written
through `ILogger` would be exported twice.

With `UseSerilog()` or `services.AddSerilog()`, Serilog replaces the
`ILoggerFactory`, and the bridge also exports events written through `ILogger`.

The bridge skips a logger that already sends its events to OpenTelemetry,
through `Serilog.Sinks.OpenTelemetry` or through `ILogger` providers that
include an OpenTelemetry logger provider
(for example `UseSerilog(writeToProviders: true)`).

## `Serilog` log events conversion

`Serilog`'s `LogEvent`s are converted to OpenTelemetry log records in
the following way:

- `Timestamp` is set as a `Timestamp`
- The name of `Level` is set as a `SeverityText`
- `Level` is mapped to `SeverityNumber` as outlined in the next section
- `MessageTemplate.Text` is set as a `Body`
- The `SourceContext` property is set as an `InstrumentationScope.Name`.
  Without it, the scope name is empty.
- Other properties are added as attributes with the same names.
  Structures and dictionaries become maps, sequences become arrays, and
  non-primitive scalars such as `DateTime` become invariant-culture strings.
  Values that cannot be nested, such as an array in an array, are exported
  as Serilog text, for example `[1, 2]`.
- If `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE` is `true`,
  the rendered message is added as the `serilog.rendered_message` attribute
- `Exception` is used to populate the following properties: `exception.type`,
  `exception.message`, `exception.stacktrace`
- The trace context comes from `Activity.Current`

### `Serilog` level severity mapping

| `LogEventLevel` | `LogRecordSeverity` |
|-----------------|---------------------|
| `Verbose`       | `Trace`             |
| `Debug`         | `Debug`             |
| `Information`   | `Info`              |
| `Warning`       | `Warn`              |
| `Error`         | `Error`             |
| `Fatal`         | `Fatal`             |

## Known limitations

If a logger writes to `WriteTo.Logger(existingLogger)`, events written
directly to `existingLogger` are not exported.
