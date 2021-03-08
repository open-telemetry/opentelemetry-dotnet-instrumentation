#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/../.." >/dev/null && pwd )"

export CORECLR_ENABLE_PROFILING="1"
export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
export CORECLR_PROFILER_PATH="${DIR}/src/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native/obj/Debug/x64/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so"
export OTEL_DOTNET_TRACER_HOME="${DIR}"
export OTEL_INTEGRATIONS="${OTEL_DOTNET_TRACER_HOME}/integrations.json"

eval "$@"
