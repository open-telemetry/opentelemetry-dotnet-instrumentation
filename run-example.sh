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
vendorPluginTargetFramework=${aspNetAppTargetFramework}
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
tracesExporter=${tracesExporter:-otlp}
metricsExporter=${metricsExporter:-otlp}
exampleAppInjectSDK=${exampleAppInjectSDK:-true}

# Build the applications
if [[ $skipAppBuild != "true" && $skipAppBuild != "1" ]]; then
  # Build the server app
  dotnet publish -f $aspNetAppTargetFramework -c $configuration ./examples/AspNetCoreMvc/Examples.AspNetCoreMvc.csproj

  # build plugin for HTTP server app
  dotnet publish -f $vendorPluginTargetFramework -c $configuration ./examples/Vendor.Distro/Examples.Vendor.Distro.csproj -o bin/tracer-home/$vendorPluginTargetFramework

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

# disable console exporters to avoid noise
export OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED=false
export OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED=false
export OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED=false

# instrument and run HTTP server app in background
export OTEL_DOTNET_AUTO_PLUGINS="Examples.AspNetCoreMvc.OtelSdkPlugin, Examples.AspNetCoreMvc, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null:Examples.Vendor.Distro.Plugin, Examples.Vendor.Distro, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null"
ENABLE_PROFILING=${enableProfiling} OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES="MyCompany.MyProduct.MyLibrary" OTEL_SERVICE_NAME="aspnet-server" OTEL_TRACES_EXPORTER=${tracesExporter} OTEL_METRICS_EXPORTER=${metricsExporter} OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES=none ./dev/instrument.sh ASPNETCORE_URLS="http://127.0.0.1:8080/" dotnet ./examples/AspNetCoreMvc/bin/${configuration}/${aspNetAppTargetFramework}/Examples.AspNetCoreMvc.dll &
unset OTEL_DOTNET_AUTO_PLUGINS
./dev/wait-local-port.sh 8080

# instrument and run HTTP client app
ENABLE_PROFILING=${enableProfiling} OTEL_SERVICE_NAME=${exampleApp} OTEL_TRACES_EXPORTER=${tracesExporter} OTEL_DOTNET_AUTO_LOAD_TRACER_AT_STARTUP=${exampleAppInjectSDK} OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES=none ./dev/instrument.sh $exampleAppDotnetCli ./examples/${exampleApp}/bin/$configuration/${exampleAppTargetFramework}/Examples.${exampleApp}.${exampleAppExt}

# verify if it works
{
  echo "Check traces at http://localhost:16686/search"
  echo "Check metrics at http://localhost:8889/metrics"
  echo "Press enter to close containers and stop example apps"
  read
} 2> /dev/null
