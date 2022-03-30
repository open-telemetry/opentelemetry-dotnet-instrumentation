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
exampleAppTargetFramework=${exampleAppTargetFramework:-netcoreapp3.1}
exampleApp=${exampleApp:-ConsoleApp}

# Handle the differences between launching a dll and exe
exampleAppExt="dll"
exampleAppDotnetCli="dotnet"
if [[ $exampleAppTargetFramework == net4* ]]; then
  exampleAppExt="exe"
  exampleAppDotnetCli=""
fi

# Defaults for selected OTEL env vars.
exporter=${exporter:-otlp}
exampleAppInjectSDK=${exampleAppInjectSDK:-true}

# Build the applications
if [[ $skipAppBuild != "true" && $skipAppBuild != "1" ]]; then
  # Build the server app
  dotnet publish -f $aspNetAppTargetFramework -c $configuration ./examples/AspNetCoreMvc/Examples.AspNetCoreMvc.csproj

  # build plugin for HTTP server app
  dotnet publish -f $aspNetAppTargetFramework -c $configuration ./examples/Vendor.Distro/Examples.Vendor.Distro.csproj -o bin/tracer-home/$aspNetAppTargetFramework

  # build the client app
  dotnet publish -f $exampleAppTargetFramework -c $configuration ./examples/${exampleApp}/Examples.${exampleApp}.csproj
fi

function finish {
  if [[ $keepContainers != "true" && $keepContainers != "1" ]]; then
    docker-compose -f ./dev/docker-compose.yaml -f ./examples/docker-compose.yaml down
  fi
  kill 0 # kill background processes
}
trap finish EXIT

# Start the Docker containers
docker-compose -f ./dev/docker-compose.yaml -f ./examples/docker-compose.yaml up -d

# instrument and run HTTP server app in background
export OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS="Examples.AspNetCoreMvc.OtelSdkPlugin, Examples.AspNetCoreMvc, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null:Examples.Vendor.Distro.Plugin, Examples.Vendor.Distro, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null"
ENABLE_PROFILING=${enableProfiling} OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4318" OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS="AspNet,SqlClient" OTEL_SERVICE_NAME="aspnet-server" OTEL_TRACES_EXPORTER=${exporter} ./dev/instrument.sh ASPNETCORE_URLS="http://127.0.0.1:8080/" dotnet ./examples/AspNetCoreMvc/bin/${configuration}/${aspNetAppTargetFramework}/Examples.AspNetCoreMvc.dll &
unset OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS
./dev/wait-local-port.sh 8080

# instrument and run HTTP client app
ENABLE_PROFILING=${enableProfiling} OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4318" OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS="HttpClient" OTEL_SERVICE_NAME="http-client" OTEL_TRACES_EXPORTER=${exporter} OTEL_DOTNET_AUTO_LOAD_AT_STARTUP=${exampleAppInjectSDK} ./dev/instrument.sh $exampleAppDotnetCli ./examples/${exampleApp}/bin/$configuration/${exampleAppTargetFramework}/Examples.${exampleApp}.${exampleAppExt}

# verify if it works
read -p "Check traces under: http://localhost:16686/search. Press enter to close containers and stop example apps"
