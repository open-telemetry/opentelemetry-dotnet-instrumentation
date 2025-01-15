FROM mcr.microsoft.com/dotnet/sdk:9.0.102-bookworm-slim@sha256:84fd557bebc64015e731aca1085b92c7619e49bdbe247e57392a43d92276f617

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "48e5763854527aca84bf2c9b1542a22ec490e85657725eac8dc18eb0ed809413  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.404 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
