# `log4net` trace context injection

> [!IMPORTANT]
> log4net trace context injection is an experimental feature.

The `log4net` trace context injection is enabled by default. It can be disabled by setting `OTEL_DOTNET_AUTO_LOGS_LOG4NET_INSTRUMENTATION_ENABLED` to `false`.

Context injection is supported for `log4net` in versions >= 2.0.13 && < 4.0.0

Following properties are set by default on the collection of logging event's properties:

- `trace_id`
- `span_id`
- `trace_flags`

This allows for trace context to be logged into currently configured log destination, e.g. a file.
In order to use them, pattern needs to be updated.