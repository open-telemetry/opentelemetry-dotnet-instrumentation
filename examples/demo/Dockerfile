FROM mcr.microsoft.com/dotnet/sdk:8.0.412-jammy@sha256:10a559c5e8e315416a1ab84547c96859273f48c88a8b70faf0710ab98a15fafd

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
