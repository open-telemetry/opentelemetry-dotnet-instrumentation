# Log to trace correlation

## [Logs bridges](https://opentelemetry.io/docs/specs/otel/glossary/#log-appender--bridge)

### `ILogger`

> [!NOTE]
> Automatic log to trace correlation provided by OpenTelemetry .NET Automatic Instrumentation
> currently works only for .NET applications using `Microsoft.Extensions.Logging`.
> See [#2310](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2310)
> and [config](./config.md#logs-instrumentations) for more details.

OpenTelemetry .NET SDK automatically correlates logs to trace data.
When logs are emitted in the context of an active trace, trace context
[fields](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/logs/data-model.md#trace-context-fields)
`TraceId`, `SpanId`, `TraceState` are automatically populated.

The following are logs produced by the sample console
[application](../examples/demo/Service/Program.cs):

```json
"logRecords": [
    {
        "timeUnixNano": "1679392614538226700",
        "severityNumber": 9,
        "severityText": "Information",
        "body": {
            "stringValue": "Success! Today is: {Date:MMMM dd, yyyy}"
        },
        "flags": 1,
        "traceId": "21df288eada1ce4ace6c40f39a6d7ce1",
        "spanId": "a80119e5a05fed5a"
    }
]
```

Further reading:

- [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs/correlation)
- [OpenTelemetry Specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/logs/data-model.md#trace-context-fields)

### `log4net`

See [`log4net-bridge`](./log4net-bridge.md).

## `log4net` trace context injection

> [!IMPORTANT]
> log4net trace context injection is an experimental feature.

The `log4net` trace context injection is enabled by default.
It can be disabled by setting
`OTEL_DOTNET_AUTO_LOGS_LOG4NET_INSTRUMENTATION_ENABLED` to `false`.

Context injection is supported for `log4net` in versions >= 2.0.13 && < 4.0.0

Following properties are set by default on the collection of logging event's properties:

- `trace_id`
- `span_id`
- `trace_flags`

This allows for trace context to be logged into currently configured log destination,
 e.g. a file. In order to use them, pattern needs to be updated.

### `NLog`

See [`nlog-bridge`](./nlog-bridge.md).

## `NLog` trace context injection

> [!IMPORTANT]
> NLog trace context injection is an experimental feature.

The `NLog` trace context injection is enabled by default.
It can be disabled by setting `OTEL_DOTNET_AUTO_LOGS_NLOG_INSTRUMENTATION_ENABLED` to `false`.

Context injection is supported for `NLOG` in versions >= 5.0.0 && < 7.0.0

Following properties are set by default on the collection of logging event's properties:

- `trace_id`
- `span_id`
- `trace_flags`

This allows for trace context to be logged into currently configured log destination,
 e.g. a file. In order to use them, pattern needs to be updated.