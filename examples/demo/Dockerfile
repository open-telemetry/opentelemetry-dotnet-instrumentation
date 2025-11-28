FROM mcr.microsoft.com/dotnet/sdk:8.0.416-jammy@sha256:71217e2029c9844f13aa98114ae4aea9e77287cd8c152f7b7c09f8af61a1affa

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.13.0
ENV OTEL_DOTNET_AUTO_HOME=/otel-dotnet-auto
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
