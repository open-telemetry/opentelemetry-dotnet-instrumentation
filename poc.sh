#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
cd $DIR

aspNetAppTargetFramework=${aspNetAppTargetFramework:-netcoreapp3.1}
consoleAppTargetFramework=${consoleAppTargetFramework:-netcoreapp3.1}

function finish {
  docker stop jaeger # stop Jaeger
  kill 0 # kill background processes
}
trap finish EXIT

# build managed and native code
./build.sh

# start Jaeger
docker run -d --rm --name jaeger \
  -e COLLECTOR_ZIPKIN_HOST_PORT=:9411 \
  -p 5775:5775/udp \
  -p 6831:6831/udp \
  -p 6832:6832/udp \
  -p 5778:5778 \
  -p 16686:16686 \
  -p 14268:14268 \
  -p 14250:14250 \
  -p 9411:9411 \
  jaegertracing/all-in-one:1.22

# instrument and run HTTP server app in background
ASPNETCORE_URLS="http://127.0.0.1:8080/" ./dev/instrument.sh dotnet run --no-launch-profile -f $aspNetAppTargetFramework -p ./samples/Samples.AspNetCoreMvc31/Samples.AspNetCoreMvc31.csproj &
./dev/wait-local-port.sh 8080

# instrument and run HTTP client app
time ./dev/instrument.sh dotnet run --no-launch-profile -f $consoleAppTargetFramework -p ./samples/ConsoleApp/ConsoleApp.csproj

# verify if it works
read -p "Check traces under: http://localhost:16686/search. Press enter to continue"
