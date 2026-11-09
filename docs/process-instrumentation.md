# Process instrumentation

The experimental `PROCESS` trace instrumentation connects telemetry across
process boundaries. It extracts incoming context from the process environment
and injects outgoing context into child process environments using the configured
propagators. It supports .NET 8–10 and .NET Framework and implements
[environment context propagation](https://github.com/open-telemetry/opentelemetry-specification/issues/5398).

## Configuration

See the [environment variable reference](config.md#process-environment-context-propagation)
for settings and defaults, or use the corresponding
[YAML options](file-based-configuration.md#instrumentation-options).

Extraction and injection are independent. Set the extraction mode to `off` for
injection only, or disable injection for extraction only. Disabling `PROCESS`
disables both directions while leaving ordinary configured method spans enabled.

Unsupported extraction modes are logged and use `parent`, or fail initialization
when fail-fast is enabled. Use `OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED=true` for
environment configuration, or `fail_fast: true` in YAML.

Startup-hook-only deployments leave this instrumentation inactive and log an
explanatory message.

## Incoming context and invocation spans

Extraction uses the process environment captured at startup. Incoming trace
context is applied to spans for command or job methods selected through
[configuration based instrumentation](nocode-instrumentation.md). Choose a method
whose execution covers one invocation.

Method targets are configured in YAML under
`instrumentation/development.dotnet.no_code.targets`; environment-only
configuration cannot define targets. YAML can reference environment variables for
the process options. See the
[invocation configuration example](file-based-configuration.md#process-invocation-spans).

When the selected method starts without an ambient `Activity.Current`, its span
receives the incoming context according to the extraction mode:

| Mode | Behavior |
| ---- | -------- |
| `parent` | Uses the incoming trace context as a remote parent. |
| `link` | Adds a link to the incoming trace context. |
| `off` | Leaves the method span unchanged and disables incoming baggage initialization. |

Parent and link inputs are supplied through the public
`ActivitySource.StartActivity(...)` API before sampling and ID generation.
Existing ambient parents take precedence. Nested method spans and other
activities created within the invocation inherit normally. The agent does not
change the parents or links of other `ActivitySource` roots.

The invocation span lasts until the selected method returns, or until its
returned `Task` or `Task<T>` completes. Choose a method that awaits the command
or job's work, rather than one that only schedules background work. Spans use
`OpenTelemetry.AutoInstrumentation.NoCode`, which the agent registers
automatically. No additional process-wide or `Main` span is created.

Without a selected target, `PROCESS` can initialize baggage and inject child
environments but creates no invocation spans.

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
