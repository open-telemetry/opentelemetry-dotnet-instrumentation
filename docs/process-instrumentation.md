# Process instrumentation

The `PROCESS` trace instrumentation connects telemetry across
process boundaries. It extracts incoming context from the process environment
and injects outgoing context into child process environments using the configured
propagators. It implements
[environment context propagation](https://github.com/open-telemetry/opentelemetry-specification/issues/5398).

## Configuration

See the [environment variable reference](config.md#process-environment-context-propagation)
for settings and defaults, or use the corresponding
[YAML options](file-based-configuration.md#instrumentation-options).

Extraction and injection are independent. Set the extraction mode to `off` for
injection only, or disable injection for extraction only. Disabling `PROCESS`
disables both directions. Existing Process metrics are configured independently.

Unsupported extraction modes are logged and use `parent`, or fail initialization
when fail-fast is enabled. Use `OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED=true` for
environment configuration, or `fail_fast: true` in YAML.

Startup-hook-only deployments leave this instrumentation inactive and log an
explanatory message.

## Incoming context and activities

Extraction uses the process environment captured once at startup. Incoming trace
context applies automatically to root activities created through
`System.Diagnostics.ActivitySource.StartActivity(...)` or
`ActivitySource.CreateActivity(...)`, including the simple overload:

``` csharp
using System.Diagnostics;

using var source = new ActivitySource("MyCommand");
using var job = source.StartActivity("job");
// Run the job within this activity's scope.
```

Register your source with the tracer provider as usual. For the agent's provider,
set `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES=MyCommand`, or use YAML
[`traces.additional_sources`](file-based-configuration.md#instrumentation-additional-sources).
An application-owned provider can register it with `AddSource("MyCommand")`.
Process instrumentation does not subscribe to additional sources or force
activities to be created when there are no listeners.

When there is no ambient `Activity.Current`, supplied parent context, or supplied
parent ID, the activity receives the incoming trace context according to the
extraction mode:

| Mode | Behavior |
| ---- | -------- |
| `parent` | Uses the incoming trace context as a remote parent, including its trace flags and trace state. |
| `link` | Adds a link to the incoming trace context, preserving existing links and avoiding a duplicate incoming link. |
| `off` | Leaves activity creation unchanged and disables incoming baggage initialization. |

Incoming parents and links are available to listeners and samplers before the
activity is created. The configured sampler still determines whether the new
activity is recorded and sampled. Ambient activities and supplied parents take
precedence. A supplied parent ID, including an empty string used to request an
independent root, is preserved. A default `ActivityContext` with no parent ID
uses the incoming context when no activity is current.

The startup context can apply to multiple root activities; it is not consumed by
the first one. Nested activities inherit their active parent normally. With
`CreateActivity(...)`, extraction is applied during creation and sampling, even
if the application calls `Activity.Start()` later. Normal activity lifetimes
remain under application control. No additional process-wide or `Main` span is
created.

[Configuration based instrumentation](nocode-instrumentation.md) is an optional
way to create method spans without changing application code. Its spans receive
incoming context through the same activity factory hook; no method targets or
YAML configuration are required for manually created activities.

### Implementation and support boundaries

The profiler intercepts DiagnosticSource 10.x's private nine-argument
`ActivitySource.CreateActivity` method, the shared implementation used by the
public `StartActivity` and `CreateActivity` overloads. The hook supplies parent
and link arguments before sampling and ID generation, then lets the original
method execute. It is registered only when extraction produces a valid incoming
trace context. Incoming links are cached as read-only data; application-supplied
links retain deferred enumeration. Baggage initialization and child-process
injection remain available when no valid trace context was extracted.

This hook is constrained to the supported assembly version and method signature.
It does not cover legacy activities created with `new Activity(...).Start()`.
Because context is applied to eligible roots throughout the process, use `off`
when launch context must not parent or link unrelated later work.

## Child process propagation

Context injection supports direct launches through
`System.Diagnostics.Process.Start(...)`, including static overloads and the
instance `Process.Start()` method using `Process.StartInfo`. Launch settings
come from `System.Diagnostics.ProcessStartInfo`, including its `Environment`
or .NET Framework `EnvironmentVariables` collection.

`ProcessStartInfo.UseShellExecute` must be `false`. Shell-executed launches are
skipped, including .NET Framework launches that use the default value `true`.
The hooks intercept `Process.StartCore(ProcessStartInfo)` on .NET and
`Process.StartWithCreateProcess(ProcessStartInfo)` on .NET Framework.

Injection uses the current context at each process launch, without creating a
process span. It replaces the configured propagator's fields in a copy of the
child environment, preserving the parent environment and the original
`ProcessStartInfo`.

Without an active span, `parent` mode forwards the valid startup trace context;
other modes omit trace context. When the configured propagators support baggage,
injection uses live ambient baggage, including changes made by the application.

## Propagators and environment variable names

Both directions use the configured [propagators](config.md#propagators). With the
default `OTEL_PROPAGATORS=tracecontext,baggage`, the fields map to `TRACEPARENT`,
`TRACESTATE`, and `BAGGAGE`. Selecting different propagators changes the fields
extracted and injected; for example, `b3` uses `B3`. Optional fields, such as
`TRACESTATE`, are written only when present in the context. File-based
configuration selects propagators through its
[`propagator` configuration](file-based-configuration.md#propagator-configuration).

Propagator field names are mapped to environment variable names by converting
ASCII letters to uppercase and replacing characters other than ASCII letters,
digits, and underscores with underscores. A leading digit is prefixed with an
underscore. For example, `traceparent` becomes `TRACEPARENT`, and `x-b3-sampled`
becomes `X_B3_SAMPLED`.

Lookup follows platform rules: names are case-sensitive on Unix and
case-insensitive on Windows. On Unix, `traceparent` does not substitute for
`TRACEPARENT`.

## Baggage inheritance

In `parent` and `link` modes, extracted baggage initializes the agent's
initialization execution context, with existing ambient values taking precedence.
It then follows the SDK's normal execution-context inheritance and application
updates. Unrelated or flow-suppressed contexts have no initialization guarantee.
The agent does not reset baggage for each invocation.
