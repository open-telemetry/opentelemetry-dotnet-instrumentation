#!/bin/bash

# guess OS_TYPE if not provided
OS_TYPE=${OS_TYPE:-}
if [ -z "$OS_TYPE" ]; then
  case "$(uname -s | tr '[:upper:]' '[:lower:]')" in
    cygwin_nt*|mingw*|msys_nt*)
      OS_TYPE="windows"
      ;;
    linux*)
      if [ "$(ldd /bin/ls | grep -m1 'musl')" ]; then
        OS_TYPE="linux-musl"
      else
        OS_TYPE="linux-glibc"
      fi
      ;;
    darwin*)
      OS_TYPE="macos"
      ;;
  esac
fi

# validate input
case "$OS_TYPE" in
  "linux-glibc")
    DOTNET_RUNTIME_ID="linux-x64"
    ;;
  "linux-musl")
    DOTNET_RUNTIME_ID="linux-musl-x64"
    ;;
  "macos")
    DOTNET_RUNTIME_ID="osx-x64"
    ;;
  "windows")
    ;;
  *)
    echo "Set the operating system type using the OS_TYPE environment variable. Supported values: linux-glibc, linux-musl, macos, windows." >&2
    return 2
    ;;
esac

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
ENABLE_PROFILING=${ENABLE_PROFILING:-1}

# Configure .NET Core runtime
export DOTNET_ADDITIONAL_DEPS="${CURDIR}/bin/tracer-home/AdditionalDeps"
export DOTNET_SHARED_STORE="${CURDIR}/bin/tracer-home/store"
export DOTNET_STARTUP_HOOKS="${CURDIR}/bin/tracer-home/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll"

# Configure ASP.NET Core startup
export ASPNETCORE_HOSTINGSTARTUPASSEMBLIES="OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper"

# Configure OpenTelemetry .NET Auto-Instrumentation
export OTEL_DOTNET_AUTO_HOME="${CURDIR}/bin/tracer-home"
export OTEL_DOTNET_AUTO_INTEGRATIONS_FILE="${CURDIR}/bin/tracer-home/integrations.json"
export OTEL_DOTNET_AUTO_DEBUG="1"
export OTEL_DOTNET_AUTO_DUMP_ILREWRITE_ENABLED="0"
export OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES=${OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES:-dotnet.exe,dotnet}

# Enable console exporters
export OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED=${OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED:-true}
export OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED=${OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED:-true}
export OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED=${OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED:-true}

# Enable .NET Framework Profiling API
if [ "$OS" == "windows" ]
then
    export COR_ENABLE_PROFILING="${ENABLE_PROFILING}"
    export COR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
    export COR_PROFILER_PATH="${CURDIR}/bin/tracer-home/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
    # Set paths for both bitness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
    export COR_PROFILER_PATH_64="${CURDIR}/bin/tracer-home/win-x64/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
    export COR_PROFILER_PATH_32="${CURDIR}/bin/tracer-home/win-x86/OpenTelemetry.AutoInstrumentation.Native.${SUFIX}"
fi

# Enable .NET Core Profiling API
export CORECLR_ENABLE_PROFILING="${ENABLE_PROFILING}"
export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
  if [ "$OS_TYPE" == "windows" ]
  then
    # Set paths for both bitness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
    export CORECLR_PROFILER_PATH_64="$OTEL_DOTNET_AUTO_HOME/win-x64/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
    export CORECLR_PROFILER_PATH_32="$OTEL_DOTNET_AUTO_HOME/win-x86/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
  else
    export CORECLR_PROFILER_PATH="$OTEL_DOTNET_AUTO_HOME/$DOTNET_RUNTIME_ID/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
  fi
