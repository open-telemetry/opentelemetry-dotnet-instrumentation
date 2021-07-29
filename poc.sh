#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
cd $DIR

aspNetAppTargetFramework=${aspNetAppTargetFramework:-netcoreapp3.1}
sampleAppTargetFramework=${sampleAppTargetFramework:-netcoreapp3.1}
sampleApp=${sampleApp:-ConsoleApp}

function finish {
  docker stop jaeger # stop Jaeger
  docker stop redis # stop Redis
  docker stop mongo # stop MongoDb
  kill 0 # kill background processes
}
trap finish EXIT

# copy profiler to good location
cp bin/tracer-home/win-x64/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll bin/tracer-home/

# build plugin for HTTP server app
dotnet publish -f $aspNetAppTargetFramework samples/Vendor.Distro/Vendor.Distro.csproj -o bin/tracer-home/$aspNetAppTargetFramework

# start mongodb
docker run -d --rm --name mongo \
  -p 27017:27017 \
  mongo:4.4.6

# start redis
docker run -d --rm --name redis \
  -p 6379:6379 \
  redis:6.2.4

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
export OTEL_DOTNET_TRACER_INSTRUMENTATION_PLUGINS="Samples.AspNetCoreMvc.OtelSdkPlugin, Samples.AspNetCoreMvc31, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null:Vendor.Distro.Plugin, Vendor.Distro, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null"
./dev/instrument.sh OTEL_DOTNET_TRACER_INSTRUMENTATIONS="AspNet,SqlClient,MongoDb" OTEL_SERVICE="aspnet-server" ASPNETCORE_URLS="http://127.0.0.1:8080/" dotnet run --no-launch-profile -f $aspNetAppTargetFramework -p ./samples/Samples.AspNetCoreMvc31/Samples.AspNetCoreMvc31.csproj &
unset OTEL_DOTNET_TRACER_INSTRUMENTATION_PLUGINS
./dev/wait-local-port.sh 8080

# instrument and run HTTP client app
time ./dev/instrument.sh OTEL_DOTNET_TRACER_INSTRUMENTATIONS="HttpClient" OTEL_SERVICE="http-client" dotnet run --no-launch-profile -f $sampleAppTargetFramework -p ./samples/${sampleApp}/${sampleApp}.csproj

# verify if it works
read -p "Check traces under: http://localhost:16686/search. Press enter to continue"
