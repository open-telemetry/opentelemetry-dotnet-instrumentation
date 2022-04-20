# Configuration

This document lists advanced configuration settings rarely changed by users.

## Environment

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_AZURE_APP_SERVICES` | Set to indicate that the profiler is running in the context of Azure App Services. | `false` |

## Diagnostics
| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_DUMP_ILREWRITE_ENABLED` | Lets the profiler dump the IL original code and modification to the log. | `false` |

## CLR Optimizations

Bytecode instrumentations are implemented by registering a CLR Profiler, receiving
notifications from the .NET runtime, and rewriting methods at runtime to invoke the
appropriate instrumentation. The CLR Profiler components of this library have been
authored to run with as much of the CLR optimizations enabled as possible, but under
rare circumstances it is possible that the optimizations have caused rewriting not to
run, resulting in missing spans. To determine whether CLR optimizations are
affecting instrumentations, you may set the following configurations to modify CLR optimizations.

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_CLR_DISABLE_OPTIMIZATIONS` |  Set to `true` to disable all JIT optimizations. | `false` |
| `OTEL_DOTNET_AUTO_CLR_ENABLE_INLINING` | Set to `false` to disable JIT inlining. | `true` |
| `OTEL_DOTNET_AUTO_CLR_ENABLE_NGEN` | Set to `false` to disable NGEN images. | `true` |