# Development Environment

Run `export dev/envvars.sh` in your shell (e.g. bash, zsh) where you want to run the .NET instrumented apps. Run it from the root of this repository.

Run `docker-compose up` to run OTel Collector and Jaeger. Run it from this directory. You should see trace logs in OTel Collector output.

Navigate to http://localhost:16686/search to see the collected traces.

Navigate to http://localhost:8889/metrics to see the collected  metrics.

Navigate to http://localhost:13133 to see the collector's health.
