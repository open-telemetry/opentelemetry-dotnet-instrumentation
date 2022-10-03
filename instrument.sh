#!/bin/sh

# validate input
case "$DISTRIBUTION" in
  "linux-glibc"|"linux-musl"|"macos"|"windows")
    ;;
  *)
    echo "Please specify the distribution by setting the DISTRIBUTION env var. Supported values: linux-glibc, linux-musl, macos, windows." >&2
    return 2
    ;;
esac

case "$ENABLE_PROFILING" in
  "true"|"false")
    ;;
  "")
    ENABLE_PROFILING="true"
    ;;
  *)
    echo "Invalid ENABLE_PROFILING env var. Supported values: true, false." >&2
    return 2
    ;;
esac

# set defaults
test -z "$INSTALL_DIR" && INSTALL_DIR="./otel-dotnet-auto"


# check $INSTALL_DIR and use it to set the absolute path $OTEL_DIR 
if [ -z "$(ls -A $INSTALL_DIR)" ]; then
  echo "There are no files under the location specified via INSTALL_DIR."
  return 1
fi
if [ "$DISTRIBUTION" == "macos" ]; then
  OTEL_DIR=$(greadlink -fn $INSTALL_DIR)
else
  OTEL_DIR=$(readlink -fn $INSTALL_DIR)
fi
if [ "$DISTRIBUTION" == "windows" ]; then
  OTEL_DIR=$(cygpath -w $OTEL_DIR)
fi
if [ -z "$OTEL_DIR" ]; then
  echo "Failed to get INSTALL_DIR absolute path. "
  return 1
fi

if [ "$ENABLE_PROFILING" = "true" ]; then
  # Set the .NET CLR Profiler file sufix
  case "$DISTRIBUTION" in
    "linux-glibc"|"linux-musl")
      SUFIX="so"
      ;;
    "macos")
      SUFIX="dylib"
      ;;
    "windows")
      SUFIX="dll"
      ;;
    *)
      echo "BUG: Unknown distribution. Please submit an issue in https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation." >&2
      return 1
      ;;
  esac

  # Enable .NET Framework Profiling API
  if [ "$DISTRIBUTION" == "windows" ]
  then
    export COR_ENABLE_PROFILING="$ENABLE_PROFILING"
    export COR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
    # Set paths for both bitness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
    export COR_PROFILER_PATH_64="$OTEL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
    export COR_PROFILER_PATH_32="$OTEL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
  fi

  # Enable .NET Core Profiling API
  export CORECLR_ENABLE_PROFILING="${ENABLE_PROFILING}"
  export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
  if [ "$DISTRIBUTION" == "windows" ]
  then
    # Set paths for both bitness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
    export CORECLR_PROFILER_PATH_64="$OTEL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
    export CORECLR_PROFILER_PATH_32="$OTEL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
  else
    export CORECLR_PROFILER_PATH="$OTEL_DIR/OpenTelemetry.AutoInstrumentation.Native.$SUFIX"
  fi

  # Configure the bytecode instrumentation configuration file
  export OTEL_DOTNET_AUTO_INTEGRATIONS_FILE="$OTEL_DIR/integrations.json"
fi

# Configure .NET Core Runtime
export DOTNET_ADDITIONAL_DEPS="$OTEL_DIR/AdditionalDeps"
export DOTNET_SHARED_STORE="$OTEL_DIR/store"
export DOTNET_STARTUP_HOOKS="$OTEL_DIR/netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll"

# Configure OpenTelemetry .NET Auto-Instrumentation
export OTEL_DOTNET_AUTO_HOME="$OTEL_DIR"
