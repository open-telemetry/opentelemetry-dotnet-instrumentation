#!/bin/bash
set -euxo pipefail

mkdir -p /var/log/opentelemetry/dotnet
touch /var/log/opentelemetry/dotnet/dotnet-tracer-native.log
tail -f /var/log/opentelemetry/dotnet/dotnet-tracer-native.log | awk '
  /info/ {print "\033[32m" $0 "\033[39m"}
  /warn/ {print "\033[31m" $0 "\033[39m"}
' &

eval "$@"
