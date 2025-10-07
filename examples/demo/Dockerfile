FROM mcr.microsoft.com/dotnet/sdk:8.0.414-jammy@sha256:b6142e9f2ba1d33700791de1b5946dd3f75d65198ca73be99aca966ee37896f1

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.12.0
ENV OTEL_DOTNET_AUTO_HOME=/otel-dotnet-auto
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
