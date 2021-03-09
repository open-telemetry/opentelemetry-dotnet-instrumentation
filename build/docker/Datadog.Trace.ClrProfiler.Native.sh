#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd "$DIR/../.."

PUBLISH_OUTPUT_NET2="$( pwd )/src/bin/managed-publish/netstandard2.0"
PUBLISH_OUTPUT_NET31="$( pwd )/src/bin/managed-publish/netcoreapp3.1"

cd src/Datadog.Trace.ClrProfiler.Native
mkdir -p build
(cd build && cmake ../ -DCMAKE_BUILD_TYPE=${buildConfiguration:-Debug}  && make)

mkdir -p bin/${buildConfiguration:-Debug}/x64
cp -f build/bin/Datadog.Trace.ClrProfiler.Native.so bin/${buildConfiguration:-Debug}/x64/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so

mkdir -p bin/${buildConfiguration:-Debug}/x64/netstandard2.0
cp -f $PUBLISH_OUTPUT_NET2/*.dll bin/${buildConfiguration:-Debug}/x64/netstandard2.0/

mkdir -p bin/${buildConfiguration:-Debug}/x64/netcoreapp3.1
cp -f $PUBLISH_OUTPUT_NET31/*.dll bin/${buildConfiguration:-Debug}/x64/netcoreapp3.1/