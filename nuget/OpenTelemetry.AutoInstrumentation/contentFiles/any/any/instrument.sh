#!/bin/sh

BASE_PATH="$(cd "$(dirname "$0")" && pwd)"

# Settings for .NET
export ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper
export CORECLR_ENABLE_PROFILING=1
export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
CORECLR_PROFILER_PATH="$(ls ${BASE_PATH}/OpenTelemetry.AutoInstrumentation.Native.*)"
export CORECLR_PROFILER_PATH
export DOTNET_STARTUP_HOOKS=${BASE_PATH}/OpenTelemetry.AutoInstrumentation.StartupHook.dll

# Settings for OpenTelemetry
export OTEL_DOTNET_AUTO_HOME=${BASE_PATH}
export OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED=false

exec "$@"
