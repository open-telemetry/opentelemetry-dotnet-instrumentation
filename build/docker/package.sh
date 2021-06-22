#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VERSION=0.0.1
BUILD_TYPE=${buildConfiguration:-Debug}
ARCH=${ARCHITECTURE:-x64}

mkdir -p $DIR/../../deploy/linux
for target in integrations.json defaults.env LICENSE NOTICE ; do
    cp $DIR/../../$target $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/
done
cp $DIR/../../build/artifacts/createLogPath.sh $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/

# If running the unified pipeline, copy managed assets now instead of in the profiler build step
if [ -n "${UNIFIED_PIPELINE-}" ]; then
  mkdir -p $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/netstandard2.0
  cp $DIR/../../src/bin/windows-tracer-home/netstandard2.0/*.dll $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/netstandard2.0/

  mkdir -p $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/netcoreapp3.1
  cp $DIR/../../src/bin/windows-tracer-home/netcoreapp3.1/*.dll $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/netcoreapp3.1/
fi

cd $DIR/../../deploy/linux
for pkgtype in $PKGTYPES ; do
    fpm \
        -f \
        -s dir \
        -t $pkgtype \
        -n otel-dotnet-autoinstrumentation \
        --license "Apache License, Version 2.0" \
        --provides otel-dotnet-autoinstrumentation \
        --vendor OpenTelemetry \
        -v $VERSION \
        $(if [ $pkgtype != 'tar' ] ; then echo --prefix /opt/otel-dotnet-autoinstrumentation ; fi) \
        --chdir $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64 \
        netstandard2.0/ \
        netcoreapp3.1/ \
        OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so \
        integrations.json \
        createLogPath.sh \
        defaults.env \
        LICENSE \
        NOTICE
done

gzip -f otel-dotnet-autoinstrumentation.tar

if [ -z "${MUSL-}" ]; then
  if [ "$ARCH" == "x64" ]; then
    mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION.tar.gz
  else
    mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION.$ARCH.tar.gz
  fi
else
  if [ "$ARCH" == "x64" ]; then
    mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION-musl.tar.gz
  else
    mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION-musl.$ARCH.tar.gz
  fi
fi
