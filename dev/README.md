# Development Environment

## Docker Compose

The [`docker-compose.yaml`](docker-compose.yaml) contains configuration for running OTel Collector and Jaeger.

You can run the services using from this directory by:

```sh
docker-compose up
```

You can also run it from any directory in the following way:

```sh
docker-compose -f dev/docker-compose.yaml up
```

The following Web UI endpoints are exposed:
- http://localhost:16686/search - collected traces,
- http://localhost:8889/metrics - collected metrics,
- http://localhost:13133 - collector's health.

## Instrumentation Scripts

*Caution:* Make sure that before running you have build the tracer e.g. by running:
- [`./build/docker/build.sh`](../build/docker/build.sh)
- [`./build/docker/Datadog.Trace.ClrProfiler.Native.sh`](../build/docker/Datadog.Trace.ClrProfiler.Native.sh).

[`instrument.sh`](instrument.sh) helps to run a command with .NET instrumentation in your shell (e.g. bash, zsh, git bash) .

Example usage:

```sh
./dev/instrument.sh dotnet run -f netcoreapp3.1 -p ./samples/ConsoleApp/ConsoleApp.csproj
```

 [`envvars.sh`](envvars.sh) can be used to export profiler environmental variables to your current shell session. **It has to be executed from the root of this repository**. Example usage:

 ```sh
 source ./dev/envvars.sh
 ./samples/ConsoleApp/bin/Debug/netcoreapp3.1/ConsoleApp
 ```
