# Demonstrative example

## Description

This example uses Docker Compose.
It consists of following services:

1. [`client`](Client) - console application that makes a HTTP GET request
   instrumented with OpenTelemetry .NET Automatic Instrumentation. 
3. [`service`](Service) - simple HTTP server using SQL Server.
   The application additionally has manual instrumentation (traces, metrics, logs)
   on top of the automatic instrumentation.
4. `sqlserver` - [Microsoft SQL Server](https://hub.docker.com/_/microsoft-mssql-server)
   used by `srv`
4. `otel-collector` - [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
   which collects the telemetry send by `cli` and `srv`
5. `jaeger` - [Jaeger](https://www.jaegertracing.io/) as traces backend
6. `prometheus` - [Prometheus](https://prometheus.io/) as metrics backend

## Usage

Windows (Git Bash):

```sh
docker-compose build
docker-compose up -d service
docker-compose run client
```

macOS and Linux:

```sh
make
```

The following Web UI endpoints are exposed:

- <http://localhost:16686/search> - traces
- <http://localhost:9090/graph> - metrics

You can also find the exported telemetry in the `log` directory.

## Cleanup

Windows (Git Bash):

```sh
docker-compose down --remove-orphans
rm -rf log
```

macOS and Linux:

```sh
make clean
```
