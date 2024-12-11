# `log4net` instrumentation

> [!IMPORTANT]
> log4net bridge and trace context injection are experimental features.
> Both instrumentations can be disabled by setting `OTEL_DOTNET_AUTO_LOGS_LOG4NET_INSTRUMENTATION_ENABLED` to `false`.

Both bridge and trace context injection are supported for `log4net` in versions >= 2.0.0 && < 4.0.0

## `log4net` [logs bridge](https://opentelemetry.io/docs/concepts/signals/logs/#log-appender--bridge)

The `log4net` logs bridge is disabled by default. In order to enable it, set `OTEL_DOTNET_AUTO_LOGS_ENABLE_LOG4NET_BRIDGE` to `true`.
When `log4net` logs bridge is enabled, and `log4net` is configured with at least 1 appender, application logs are exported in OTLP 
format by default to the local instance of OpenTelemetry Collector, in addition to being written into their currently configured destination (e.g. a file).

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


