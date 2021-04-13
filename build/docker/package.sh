#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VERSION=1.24.0

mkdir -p $DIR/../../deploy/linux
cp $DIR/../../integrations.json $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/
cp $DIR/../../build/artifacts/createLogPath.sh $DIR/../../src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64/

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
  mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION.tar.gz
else
  mv otel-dotnet-autoinstrumentation.tar.gz otel-dotnet-autoinstrumentation-$VERSION-musl.tar.gz
fi
