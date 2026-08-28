# IBM MQ (XMS.NET) instrumentation

## Scope

This instrumentation targets applications that talk to IBM MQ through the IBM
XMS.NET client, assembly `IBM.XMS`. Supported versions are `9.0.0` and later
`9.x` releases of `IBM.XMS`.

## Instrumented methods

| Assembly  | Type                                     | Method            | Signature                                                  | Span kind |
|-----------|-------------------------------------------|--------------------|-------------------------------------------------------------|-----------|
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageProducerImpl` | `Send`             | `Send(IMessage)`                                             | Producer  |
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageProducerImpl` | `Send`             | `Send(IMessage, DeliveryMode, Int32, Int64)`                  | Producer  |
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageProducerImpl` | `Send`             | `Send(IDestination, IMessage)`                                | Producer  |
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageProducerImpl` | `Send`             | `Send(IDestination, IMessage, DeliveryMode, Int32, Int64)`    | Producer  |
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageConsumerImpl` | `Receive`          | `Receive()`                                                   | Consumer  |
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageConsumerImpl` | `Receive`          | `Receive(Int64)`                                              | Consumer  |
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageConsumerImpl` | `ReceiveNoWait`     | `ReceiveNoWait()`                                             | Consumer  |
| `IBM.XMS` | `IBM.XMS.Client.Impl.XmsMessageListener`     | `OnMessage`        | `OnMessage(IMessage)`                                         | Consumer (async delivery) |

## Span names

Span names follow the OpenTelemetry messaging semantic convention pattern of
`{destination} {operation}`:

* `{destination} publish` for `Send` overloads.
* `{destination} receive` for `Receive` / `Receive(Int64)` / `ReceiveNoWait`.
* `{destination} deliver` for `XmsMessageListener.OnMessage`.

When the destination name cannot be resolved (for example the destination
object could not be inspected at the time the span is created), the span name
falls back to the bare operation name: `publish`, `receive`, or `deliver`.

## Attributes

| Attribute                                | Source                                                            |
|-------------------------------------------|--------------------------------------------------------------------|
| `messaging.system`                        | Always `ibmmq`.                                                    |
| `messaging.operation`                     | `publish`, `receive`, or `deliver`, matching the operation table above. |
| `messaging.destination.name`              | The resolved queue or topic name for the message.                  |
| `messaging.message.id`                    | The message's `JMSMessageID`.                                      |
| `messaging.message.conversation_id`       | The message's `JMSCorrelationID`.                                  |

## Context propagation

W3C trace context (`traceparent` and `tracestate`) is injected into outgoing
XMS messages on `Send` and extracted from incoming XMS messages on `Receive` /
`ReceiveNoWait` / `OnMessage`, carried as string message properties.

**Caveat — header name sanitization:** JMS/XMS message property names must be
valid Java identifiers. Any character in a propagator header name that falls
outside `[A-Za-z0-9_]` is replaced with `_` both when the header is injected
(written as a message property) and when it is extracted (read back as a
message property). `traceparent` and `tracestate` are themselves unaffected,
since both names are already composed only of characters in `[A-Za-z0-9_]`.
Propagators that use header names containing characters outside that set will
have those characters replaced with `_` on the wire; a non-OTel-instrumented
consumer that reads the raw XMS message properties directly will see the
sanitized property name rather than the original header name.

## Enablement

All instrumentations, including this one, are enabled by default for all
signal types (traces, metrics, and logs) — see
[`docs/config.md`](config.md#instrumentations). No action is required to turn
this instrumentation on if instrumentations are otherwise left at their
defaults.

To explicitly disable only this instrumentation for traces, set:

```bash
export OTEL_DOTNET_AUTO_TRACES_XMS_INSTRUMENTATION_ENABLED=false
```

This follows the general pattern documented in `docs/config.md`:
`OTEL_DOTNET_AUTO_TRACES_{0}_INSTRUMENTATION_ENABLED`, where `{0}` is the
uppercase instrumentation id (`XMS`), and overrides
`OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`, which in turn overrides the
global `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`. There is no variant of
these variables that takes a list of instrumentation ids to select — each
variable is a single boolean applied either to all instrumentations for a
signal (`OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`) or to one specific
instrumentation for a signal
(`OTEL_DOTNET_AUTO_TRACES_XMS_INSTRUMENTATION_ENABLED`).

## Known limitations

* Synchronous `Receive` / `Receive(Int64)` / `ReceiveNoWait` spans are created
  after the call returns, so the span's duration reflects only the measured
  call; it does not capture any additional in-flight time beyond that, and it
  does not measure the blocking wait itself.
* A `Receive` call that times out and returns `null` (no message available)
  produces no span.
* Browsing a queue via `IQueueBrowser` is not instrumented
  (`IBM.XMS.Client.Impl.XmsQueueBrowserImpl.GetEnumerator()`).
* Transacted session semantics are not modeled by this instrumentation:
  `IBM.XMS.Client.Impl.XmsSessionImpl.Commit()` and `.Rollback()` are not
  instrumented, nor is XA resource enlistment.
* .NET Framework (`netfx`) output is untested by this project; the profiler
  and managed assemblies for `netfx` are not built on macOS.

## Verified compatibility

The `amqmxmsstd` assembly's instrumented implementation type names and method
signatures are identical across IBM XMS 9.4.3 and 10.0.0 — the assembly
version tracks the product version, but the shapes this instrumentation
targets did not change between them.
