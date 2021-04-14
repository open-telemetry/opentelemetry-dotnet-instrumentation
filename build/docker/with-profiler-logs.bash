#!/bin/bash
set -euxo pipefail

mkdir -p /var/log/opentelemetry/dotnet
touch /var/log/opentelemetry/dotnet/dotnet-tracer-native.log

cleanup() {
  cat /var/log/opentelemetry/dotnet/dotnet-tracer-native* \
  | awk '
    /info/ {print "\033[32m" $0 "\033[39m"}
    /warn/ {print "\033[31m" $0 "\033[39m"}
  '
}

trap cleanup SIGINT SIGTERM EXIT

eval "$@"
