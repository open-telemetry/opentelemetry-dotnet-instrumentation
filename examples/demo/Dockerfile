FROM mcr.microsoft.com/dotnet/sdk:8.0.302-jammy@sha256:838644c2dd735cdf0ba3c6ec282b77bfea80039fb45fe41b70d2e026a1f30a12

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.9.0
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    OTEL_DOTNET_AUTO_HOME="/otel-dotnet-auto" sh otel-dotnet-auto-install.sh

WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
