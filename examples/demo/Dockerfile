FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble@sha256:2fa828c68761b1b8c23d7662dc134421b9d3b59fe1425fdbc80804e390cdb24d

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.17.0
# SHA256 published at https://github.com/cli/cli/blob/trunk/docs/install_linux.md
ARG GITHUB_CLI_KEYRING_SHA256=6084d5d7bd8e288441e0e94fc6275570895da18e6751f70f057485dc2d1a811b
ENV OTEL_DOTNET_AUTO_HOME=/otel-dotnet-auto
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl unzip && \
    mkdir -p -m 755 /etc/apt/keyrings /etc/apt/sources.list.d && \
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg -o /etc/apt/keyrings/githubcli-archive-keyring.gpg && \
    echo "${GITHUB_CLI_KEYRING_SHA256}  /etc/apt/keyrings/githubcli-archive-keyring.gpg" | sha256sum --check --status && \
    chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" > /etc/apt/sources.list.d/github-cli.list && \
    apt-get update && apt-get install -y --no-install-recommends gh && \
    rm -rf /var/lib/apt/lists/* && \
    sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
