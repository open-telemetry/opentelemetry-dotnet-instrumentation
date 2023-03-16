# Demo

## Description

This is a demonstrative example that uses Docker Compose.

It consists of following services:

1. [`client`](Client) - console application that makes a HTTP GET request
   instrumented with OpenTelemetry .NET Automatic Instrumentation.
2. [`service`](Service) - simple HTTP server using SQL Server.
   The application additionally has manual instrumentation (traces, metrics, logs)
   on top of the automatic instrumentation.
3. `sqlserver` - [Microsoft SQL Server](https://hub.docker.com/_/microsoft-mssql-server)
   used by `service`
4. `otel-collector` - [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
   which collects the telemetry send by `client` and `service`
5. `jaeger` - [Jaeger](https://www.jaegertracing.io/) as traces backend
6. `prometheus` - [Prometheus](https://prometheus.io/) as metrics backend
7. `loki` - [Grafana Loki](https://grafana.com/oss/loki/) as logs backend
8. `grafana` - [Grafana](https://grafana.com/oss/grafana/) as telemetry UI

## Usage

Windows (Git Bash):

```sh
docker compose up -d --build
```

macOS and Linux:

```sh
make
```

You can [explore](https://grafana.com/docs/grafana/v9.3/explore/)
the telemetry in [Grafana UI](http://localhost:3000/).

You can also find the exported telemetry in the `log` directory.

## Cleanup

Windows (Git Bash):

```sh
docker compose down --remove-orphans
rm -rf log
```

macOS and Linux:

```sh
make clean
```
