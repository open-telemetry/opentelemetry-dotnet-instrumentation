FROM mcr.microsoft.com/dotnet/sdk:8.0.411-jammy@sha256:e12df6fb2eaa26a07be20ba87c529b0dff4df83c7ddcba2e1ae642e529b1d5ce

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.11.0
ENV OTEL_DOTNET_AUTO_HOME=/otel-dotnet-auto
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
