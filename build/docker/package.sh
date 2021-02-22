#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VERSION=1.23.0

mkdir -p $DIR/../../deploy/linux
for target in integrations.json defaults.env LICENSE NOTICE ; do
    cp $DIR/../../$target $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64/
done
cp $DIR/../../build/artifacts/createLogPath.sh $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64/

cd $DIR/../../deploy/linux
for pkgtype in $PKGTYPES ; do
    fpm \
        -f \
        -s dir \
        -t $pkgtype \
        -n otel-instrumentation \
        --license "Apache License, Version 2.0" \
        --provides otel-dotnet-instrumentation \
        -v $VERSION \
        --prefix otel-dotnet-instrumentation \
        --chdir $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64 \
        netstandard2.0/ \
        netcoreapp3.1/ \
        OpenTelemetry.Instrumentation.ClrProfiler.Native.so \
        integrations.json \
        createLogPath.sh \
        OpenTelemetry.Instrumentation.ClrProfiler.Native.so \
        integrations.json \
        defaults.env \
        LICENSE \
        NOTICE
done

gzip -f datadog-dotnet-apm.tar

if [ -z "${MUSL-}" ]; then
  mv otel-dotnet-instrumentation.tar.gz otel-dotnet-instrumentation-$VERSION.tar.gz
else
  mv otel-dotnet-instrumentation.tar.gz otel-dotnet-instrumentation-$VERSION-musl.tar.gz
fi
