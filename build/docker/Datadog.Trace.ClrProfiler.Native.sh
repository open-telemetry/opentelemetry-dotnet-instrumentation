#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd "$DIR/../.."

PUBLISH_OUTPUT_NET2="$( pwd )/src/bin/managed-publish/netstandard2.0"
PUBLISH_OUTPUT_NET31="$( pwd )/src/bin/managed-publish/netcoreapp3.1"
BUILD_TYPE=${buildConfiguration:-Debug}

cd src/Datadog.Trace.ClrProfiler.Native
mkdir -p build
(cd build && cmake ../ -DCMAKE_BUILD_TYPE=${BUILD_TYPE}  && make)

mkdir -p bin/${BUILD_TYPE}/x64
cp -f build/bin/Datadog.Trace.ClrProfiler.Native.so bin/${BUILD_TYPE}/x64/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so

mkdir -p bin/${BUILD_TYPE}/x64/netstandard2.0
cp -f $PUBLISH_OUTPUT_NET2/*.dll bin/${BUILD_TYPE}/x64/netstandard2.0/

mkdir -p bin/${BUILD_TYPE}/x64/netcoreapp3.1
cp -f $PUBLISH_OUTPUT_NET31/*.dll bin/${BUILD_TYPE}/x64/netcoreapp3.1/