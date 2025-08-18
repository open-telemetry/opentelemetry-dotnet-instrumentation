FROM mcr.microsoft.com/dotnet/sdk:8.0.413-jammy@sha256:05594d347192d12cc8c7137493b45f9e5390aa12654cf2c45b028960b2808571

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
