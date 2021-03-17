#!/bin/bash
set -euxo pipefail

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

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
SUFIX=$(native_sufix)

cd "$DIR/../.."

PUBLISH_OUTPUT_NET2="$( pwd )/src/bin/managed-publish/netstandard2.0"
PUBLISH_OUTPUT_NET31="$( pwd )/src/bin/managed-publish/netcoreapp3.1"
BUILD_TYPE=${buildConfiguration:-Debug}

cd src/Datadog.Trace.ClrProfiler.Native
mkdir -p build
(cd build && cmake ../ -DCMAKE_BUILD_TYPE=${BUILD_TYPE}  && make)

mkdir -p bin/${BUILD_TYPE}/x64
cp -f build/bin/Datadog.Trace.ClrProfiler.Native.${SUFIX} bin/${BUILD_TYPE}/x64/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.${SUFIX}

mkdir -p bin/${BUILD_TYPE}/x64/netstandard2.0
cp -f $PUBLISH_OUTPUT_NET2/*.dll bin/${BUILD_TYPE}/x64/netstandard2.0/

mkdir -p bin/${BUILD_TYPE}/x64/netcoreapp3.1
cp -f $PUBLISH_OUTPUT_NET31/*.dll bin/${BUILD_TYPE}/x64/netcoreapp3.1/