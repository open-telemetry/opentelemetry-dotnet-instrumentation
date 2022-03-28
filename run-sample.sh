#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
cd $DIR

# Defaults for script behavior
enableProfiling=${enableProfiling:-1} # Set to zero to use only the DOTNET_STARTUP_HOOK
skipAppBuild=${skipAppBuild:-false}
keepContainers=${keepContainers:-false}

# Defaults for selected dotnet CLI commands.
configuration=${configuration:-Release}
aspNetAppTargetFramework=${aspNetAppTargetFramework:-netcoreapp3.1}
sampleAppTargetFramework=${sampleAppTargetFramework:-netcoreapp3.1}
sampleApp=${sampleApp:-ConsoleApp}

# Defaults for selected OTEL env vars.
exporter=${exporter:-otlp}
sampleAppInjectSDK=${sampleAppInjectSDK:-true}

# Build the applications
if [[ $skipAppBuild != "true" && $skipAppBuild != "1" ]]; then
  # Build the server app
  dotnet publish -f $aspNetAppTargetFramework -c $configuration ./samples/Samples.AspNetCoreMvc31/Samples.AspNetCoreMvc31.csproj

  # build plugin for HTTP server app
  dotnet publish -f $aspNetAppTargetFramework -c $configuration ./samples/Vendor.Distro/Vendor.Distro.csproj -o bin/tracer-home/$aspNetAppTargetFramework

  # build the client app
  dotnet publish -f $sampleAppTargetFramework -c $configuration ./samples/${sampleApp}/${sampleApp}.csproj
fi

function finish {
  if [[ $keepContainers != "true" && $keepContainers != "1" ]]; then
    docker-compose -f ./samples/docker-compose.yaml down
    docker-compose -f ./dev/docker-compose.yaml down
  fi
  kill 0 # kill background processes
}
trap finish EXIT

# Start the Docker containers
docker-compose -f ./dev/docker-compose.yaml up -d
docker-compose -f ./samples/docker-compose.yaml up -d

# instrument and run HTTP server app in background
export OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS="Samples.AspNetCoreMvc.OtelSdkPlugin, Samples.AspNetCoreMvc31, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null:Vendor.Distro.Plugin, Vendor.Distro, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null"
ENABLE_PROFILING=${enableProfiling} OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4318" OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS="AspNet,SqlClient" OTEL_SERVICE_NAME="aspnet-server" OTEL_TRACES_EXPORTER=${exporter} ./dev/instrument.sh ASPNETCORE_URLS="http://127.0.0.1:8080/" dotnet ./samples/Samples.AspNetCoreMvc31/bin/${configuration}/${aspNetAppTargetFramework}/Samples.AspNetCoreMvc31.dll &
unset OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS
./dev/wait-local-port.sh 8080

# instrument and run HTTP client app
ENABLE_PROFILING=${enableProfiling} OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4318" OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS="HttpClient" OTEL_SERVICE_NAME="http-client" OTEL_TRACES_EXPORTER=${exporter} OTEL_DOTNET_AUTO_LOAD_AT_STARTUP=${sampleAppInjectSDK} ./dev/instrument.sh dotnet ./samples/${sampleApp}/bin/$configuration/${sampleAppTargetFramework}/${sampleApp}.dll

# verify if it works
read -p "Check traces under: http://localhost:16686/search. Press enter to close containers and stop sample apps"
