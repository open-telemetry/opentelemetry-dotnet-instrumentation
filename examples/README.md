# Demonstrative example

## Description

This example uses Docker Compose.
It consists of following services:

1. [`cli`](cli) - console application that makes a HTTP GET request
2. [`srv`](srv) - simple HTTP server using SQL Server
3. `sqlserver` - [Microsft SQL Server](https://hub.docker.com/_/microsoft-mssql-server)
   used by `srv`
4. `otel-collector` - [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
   which collects the telemtry send by `cli` and `srv`
5. `jaeger` - [Jaeger](https://www.jaegertracing.io/) as traces backend

## Usage

```sh
docker-compose build
docker-compose up -d srv
docker-compose run cli
```

> Alternative: `make`

The following Web UI endpoints are exposed:

- <http://localhost:16686/search> - traces
- <http://localhost:8889/metrics> - metrics

You can also find the exported telemetry in the `log` directory.

## Cleanup

```sh
docker-compose down --remove-orphans
rm -rf log
```

> Alternative: `make clean`
