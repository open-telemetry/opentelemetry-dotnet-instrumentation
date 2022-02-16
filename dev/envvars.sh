#!/bin/bash

uname_os() {
    os=$(uname -s | tr '[:upper:]' '[:lower:]')
    case "$os" in
        cygwin_nt*) echo "windows" ;;
        mingw*) echo "windows" ;;
        msys_nt*) echo "windows" ;;
        *) echo "$os" ;;
    esac
}

native_sufix() {
    os=$(uname_os)
    case "$os" in
        windows*) echo "dll" ;;
        linux*) echo "so" ;;
        darwin*) echo "dylib" ;;
        *) echo "OS: ${os} is not supported" ; exit 1 ;;
    esac
}

current_dir() {
    os=$(uname_os)
    case "$os" in
        windows*) pwd -W ;;
        *) pwd ;;
    esac
}

CURDIR=$(current_dir)
SUFIX=$(native_sufix)
OS=$(uname_os)

# Enable .NET Framework Profiling API
export COR_ENABLE_PROFILING="1"
export COR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
export COR_PROFILER_PATH="${CURDIR}/bin/tracer-home/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
if [ "$OS" == "windows" ]
then
    # Set paths for both bitness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
    export COR_PROFILER_PATH_64="${CURDIR}/bin/tracer-home/win-x64/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
    export COR_PROFILER_PATH_32="${CURDIR}/bin/tracer-home/win-x86/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
fi

# Enable .NET Core Profiling API
export CORECLR_ENABLE_PROFILING="1"
export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
export CORECLR_PROFILER_PATH="${CURDIR}/bin/tracer-home/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
if [ "$OS" == "windows" ]
then
    # Set paths for both bitness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
    export CORECLR_PROFILER_PATH_64="${CURDIR}/bin/tracer-home/win-x64/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
    export CORECLR_PROFILER_PATH_32="${CURDIR}/bin/tracer-home/win-x86/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
fi
export DOTNET_STARTUP_HOOKS="${CURDIR}/bin/tracer-home/netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll"

# Configure OpenTelemetry Tracer 
export OTEL_DOTNET_AUTO_HOME="${CURDIR}/bin/tracer-home"
export OTEL_DOTNET_AUTO_INTEGRATIONS_FILE="${CURDIR}/bin/tracer-home/integrations.json"
export OTEL_DOTNET_AUTO_DEBUG="1"
export OTEL_DOTNET_AUTO_DUMP_ILREWRITE_ENABLED="0"
export OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES="dotnet.exe,dotnet"
