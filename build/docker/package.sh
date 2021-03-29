#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VERSION=1.24.0

cd "$DIR/../.."
mkdir -p deploy/linux
cp integrations.json src/Datadog.Trace.ClrProfiler.Native/bin/Release/x64/
cp build/artifacts/createLogPath.sh src/Datadog.Trace.ClrProfiler.Native/bin/Release/x64/

mkdir -p src/Datadog.Trace.ClrProfiler.Native/bin/Release/x64/netstandard2.0
cp src/bin/windows-tracer-home/netstandard2.0/*.dll src/Datadog.Trace.ClrProfiler.Native/bin/Release/x64/netstandard2.0/

mkdir -p src/Datadog.Trace.ClrProfiler.Native/bin/Release/x64/netcoreapp3.1
cp src/bin/windows-tracer-home/netcoreapp3.1/*.dll src/Datadog.Trace.ClrProfiler.Native/bin/Release/x64/netcoreapp3.1/

cd deploy/linux
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
        --prefix /opt/otel-dotnet-autoinstrumentation \
        --chdir $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/Release/x64 \
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
  mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION.tar.gz
else
  mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION-musl.tar.gz
fi
