FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble@sha256:2fa828c68761b1b8c23d7662dc134421b9d3b59fe1425fdbc80804e390cdb24d

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.17.0
ENV OTEL_DOTNET_AUTO_HOME=/otel-dotnet-auto
ENV SKIP_RELEASE_VERIFICATION=true
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
