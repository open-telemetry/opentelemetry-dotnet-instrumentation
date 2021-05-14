#!/bin/bash
set -euxo pipefail

publishTargetFramework=${publishTargetFramework:-netcoreapp3.1}

function finish {
  docker stop jaeger # stop Jaeger
  kill 0 # kill background processes
}
trap finish EXIT

# build  projects
QUICK_BUILD=1 ./build/docker/build.sh 
./build/docker/Datadog.Trace.ClrProfiler.Native.sh # probably this can be commented out if we do not touch it

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
ASPNETCORE_URLS="http://127.0.0.1:8080/" ./dev/instrument.sh dotnet run --no-launch-profile -f $publishTargetFramework -p ./test/test-applications/integrations/Samples.AspNetCoreMvc31/Samples.AspNetCoreMvc31.csproj &
sleep 10 # wait until it starts

# instrument and run HTTP client app
./dev/instrument.sh dotnet run --no-launch-profile -f $publishTargetFramework -p ./samples/ConsoleApp/ConsoleApp.csproj

# verify if it works
read -p "Check traces under: http://localhost:16686/search. Press enter to continue"
