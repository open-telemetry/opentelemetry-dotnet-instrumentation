FROM mcr.microsoft.com/dotnet/sdk:10.0.203-noble@sha256:adc02be8b87957d07208a4a3e51775935b33bad3317de8c45b1e67357b4c073b

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.15.0
ENV OTEL_DOTNET_AUTO_HOME=/otel-dotnet-auto
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
