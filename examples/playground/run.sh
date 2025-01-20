#!/bin/bash
set -eux

DOTNET=${DOTNET:-net8.0}
CONFIGURATION=${CONFIGURATION:-Debug}
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd $DIR/AspNetCoreMvc
dotnet build -c $CONFIGURATION

cd $DIR/../..
export OTEL_DOTNET_AUTO_HOME="bin/tracer-home"
. ./instrument.sh 

export OTEL_SERVICE_NAME="Examples.AspNetCoreMvc"
export OTEL_DOTNET_AUTO_PLUGINS="Examples.AspNetCoreMvc.OtelSdkPlugin, Examples.AspNetCoreMvc, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
export OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES="Examples.*"
dotnet $DIR/AspNetCoreMvc/bin/$CONFIGURATION/$DOTNET/Examples.AspNetCoreMvc.dll
