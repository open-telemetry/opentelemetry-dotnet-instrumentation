#!/bin/sh

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

# guess OS architecture if not provided
if [ -z "$ARCHITECTURE" ]; then
  case $(uname -m) in
    x86_64)  ARCHITECTURE="x64" ;;
    aarch64) ARCHITECTURE="arm64" ;;
  esac
fi

# validate architecture
case "$ARCHITECTURE" in
  "x64"|"arm64")
    ;;
  *)
    echo "Set the architecture type using the ARCHITECTURE environment variable. Supported values: x64, arm64." >&2
    return 2
    ;;
esac

# validate input
case "$OS_TYPE" in
  "linux-glibc")
    DOTNET_RUNTIME_ID="linux-$ARCHITECTURE"
    profiler_suffix="so"
    ;;
  "linux-musl")
    DOTNET_RUNTIME_ID="linux-musl-$ARCHITECTURE"
    profiler_suffix="so"
    ;;
  "macos")
    DOTNET_RUNTIME_ID="osx-x64"
    profiler_suffix="dylib"
    ;;
  "windows")
    profiler_suffix="dll"
    ;;
  *)
    echo "Set the operating system type using the OS_TYPE environment variable. Supported values: linux-glibc, linux-musl, macos, windows." >&2
    return 2
    ;;
esac

ENABLE_PROFILING=${ENABLE_PROFILING:-true}
case "$ENABLE_PROFILING" in
  "true"|"false")
    ;;
  *)
    echo "Invalid ENABLE_PROFILING. Supported values: true, false." >&2
    return 2
    ;;
esac

# set defaults
script_path="$(cd "$(dirname "$0")" && pwd)"
default_location="$HOME/.otel-dotnet-auto"
nuget_deployment=false
# if ran from a folder containing both the startup hook and native profiler assume nuget deployment
if [ -f "${script_path}/OpenTelemetry.AutoInstrumentation.StartupHook.dll" ] && \
   [ -n "$(ls -A "${script_path}"/OpenTelemetry.AutoInstrumentation.Native.*)" ]; then
  default_location="${script_path}" 
  nuget_deployment=true
fi

OTEL_DOTNET_AUTO_HOME="${OTEL_DOTNET_AUTO_HOME:=${default_location}}"

# check $OTEL_DOTNET_AUTO_HOME is a folder with actual files
if [ -z "$(ls -A "$OTEL_DOTNET_AUTO_HOME")" ]; then
  echo "There are no files under the location specified via OTEL_DOTNET_AUTO_HOME."
  return 1
fi
# get absolute path
if [ "$OS_TYPE" = "macos" ]; then
  OTEL_DOTNET_AUTO_HOME=$(greadlink -fn "$OTEL_DOTNET_AUTO_HOME")
else
  OTEL_DOTNET_AUTO_HOME=$(readlink -fn "$OTEL_DOTNET_AUTO_HOME")
fi
if [ -z "$OTEL_DOTNET_AUTO_HOME" ]; then
  echo "Failed to get OTEL_DOTNET_AUTO_HOME absolute path."
  return 1
fi
# on Windows change to Windows path format
if [ "$OS_TYPE" = "windows" ]; then
  OTEL_DOTNET_AUTO_HOME=$(cygpath -w "$OTEL_DOTNET_AUTO_HOME")
fi
if [ -z "$OTEL_DOTNET_AUTO_HOME" ]; then
  echo "Failed to get OTEL_DOTNET_AUTO_HOME absolute Windows path."
  return 1
fi

# set the platform-specific path separator (; on Windows and : on others)
if [ "$OS_TYPE" = "windows" ]; then
  SEPARATOR=";"
else
  SEPARATOR=":"
fi

# Configure OpenTelemetry .NET Automatic Instrumentation
export OTEL_DOTNET_AUTO_HOME

# Configure .NET Core Runtime
if [ -d "${OTEL_DOTNET_AUTO_HOME}/AdditionalDeps" ]; then
  DOTNET_ADDITIONAL_DEPS=${DOTNET_ADDITIONAL_DEPS:-}
  if [ -z "$DOTNET_ADDITIONAL_DEPS" ]; then
    export DOTNET_ADDITIONAL_DEPS="${OTEL_DOTNET_AUTO_HOME}/AdditionalDeps"
  else
    export DOTNET_ADDITIONAL_DEPS="${OTEL_DOTNET_AUTO_HOME}/AdditionalDeps${SEPARATOR}${DOTNET_ADDITIONAL_DEPS}"
  fi
fi

if [ -d "${OTEL_DOTNET_AUTO_HOME}/store" ]; then
  DOTNET_SHARED_STORE=${DOTNET_SHARED_STORE:-}
  if [ -z "$DOTNET_SHARED_STORE" ]; then
    export DOTNET_SHARED_STORE="${OTEL_DOTNET_AUTO_HOME}/store"
  else
    export DOTNET_SHARED_STORE="${OTEL_DOTNET_AUTO_HOME}/store${SEPARATOR}${DOTNET_SHARED_STORE}"
  fi
fi

startup_hooks_dir=$([ "$nuget_deployment" = true ] && echo "${OTEL_DOTNET_AUTO_HOME}" || echo "${OTEL_DOTNET_AUTO_HOME}/net")
DOTNET_STARTUP_HOOKS=${DOTNET_STARTUP_HOOKS:-}
if [ -z "$DOTNET_STARTUP_HOOKS" ]; then
  export DOTNET_STARTUP_HOOKS="${startup_hooks_dir}/OpenTelemetry.AutoInstrumentation.StartupHook.dll"
else
  export DOTNET_STARTUP_HOOKS="${startup_hooks_dir}/OpenTelemetry.AutoInstrumentation.StartupHook.dll${SEPARATOR}${DOTNET_STARTUP_HOOKS}"
fi

export ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper

# Configure .NET CLR Profiler
if [ "$ENABLE_PROFILING" = "true" ]; then

  profiler_dir=$([ "$nuget_deployment" = true ] && echo "${OTEL_DOTNET_AUTO_HOME}" || echo "${OTEL_DOTNET_AUTO_HOME}/${DOTNET_RUNTIME_ID}")
  # Enable .NET Framework Profiling API
  if [ "$OS_TYPE" = "windows" ]; then
    export COR_ENABLE_PROFILING="1"
    export COR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
    if [ "$nuget_deployment" = false ]; then
      # Set paths for both bit-ness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
      export COR_PROFILER_PATH_64="$OTEL_DOTNET_AUTO_HOME/win-x64/OpenTelemetry.AutoInstrumentation.Native.$profiler_suffix"
      export COR_PROFILER_PATH_32="$OTEL_DOTNET_AUTO_HOME/win-x86/OpenTelemetry.AutoInstrumentation.Native.$profiler_suffix"
    else 
      export COR_PROFILER_PATH="${profiler_dir}/OpenTelemetry.AutoInstrumentation.Native.$profiler_suffix"
    fi
  fi

  # Enable .NET Core Profiling API
  export CORECLR_ENABLE_PROFILING="1"
  export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
  if [ "$OS_TYPE" = "windows" ] && [ "$nuget_deployment" = false ]; then
    # Set paths for both bit-ness on Windows, see https://docs.microsoft.com/en-us/dotnet/core/run-time-config/debugging-profiling#profiler-location
    export CORECLR_PROFILER_PATH_64="$OTEL_DOTNET_AUTO_HOME/win-x64/OpenTelemetry.AutoInstrumentation.Native.$profiler_suffix"
    export CORECLR_PROFILER_PATH_32="$OTEL_DOTNET_AUTO_HOME/win-x86/OpenTelemetry.AutoInstrumentation.Native.$profiler_suffix"
  else
    export CORECLR_PROFILER_PATH="${profiler_dir}/OpenTelemetry.AutoInstrumentation.Native.$profiler_suffix"
  fi
fi

exec "$@"
