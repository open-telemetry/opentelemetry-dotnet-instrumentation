#!/bin/sh

BASE_PATH="$(cd "$(dirname "$0")" && pwd)"

CORECLR_PROFILER_PATH="$(ls ${BASE_PATH}/OpenTelemetry.AutoInstrumentation.Native.* 2>/dev/null)"

status=$?
if [ $status -ne 0 ]; then
  echo "Unable to locate the native profiler inside current directory, possibly due to runtime identifier not being specified when building/publishing.
Attempting to detect the runtime and use the native profiler from corresponding subdirectory of runtimes directory."

  case "$(uname -s | tr '[:upper:]' '[:lower:]')" in
    linux*)
      if [ "$(ldd /bin/ls | grep -m1 'musl')" ]; then
        OS_TYPE="linux-musl"
      else
        OS_TYPE="linux"
      fi
      FILE_EXTENSION=".so"
      ;;
    darwin*)
      OS_TYPE="osx"
      FILE_EXTENSION=".dylib"
      ;;
  esac

  case "$OS_TYPE" in
    "linux"|"linux-musl"|"osx")
      ;;
    *)
      echo "Detected operating system type not supported." >&2
      exit 1
      ;;
  esac

  case $(uname -m) in
    x86_64)  ARCHITECTURE="x64" ;;
    aarch64) ARCHITECTURE="arm64" ;;
  esac

  case "$ARCHITECTURE" in
    "x64"|"arm64")
      ;;
    *)
      echo "Detected architecture not supported." >&2
      exit 1
      ;;
  esac
  CORECLR_PROFILER_PATH="${BASE_PATH}/runtimes/${OS_TYPE}-${ARCHITECTURE}/native/OpenTelemetry.AutoInstrumentation.Native${FILE_EXTENSION}"
  if [ ! -f "${CORECLR_PROFILER_PATH}" ]; then
    echo "Unable to locate the native profiler." >&2
    exit 1
  fi
fi

export CORECLR_PROFILER_PATH

# Settings for .NET
export ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper
export CORECLR_ENABLE_PROFILING=1
export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"

export DOTNET_STARTUP_HOOKS=${BASE_PATH}/OpenTelemetry.AutoInstrumentation.StartupHook.dll

# Settings for OpenTelemetry
export OTEL_DOTNET_AUTO_HOME=${BASE_PATH}
export OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED=false

exec "$@"
