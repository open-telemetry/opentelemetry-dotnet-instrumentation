FROM mcr.microsoft.com/dotnet/sdk:8.0.302-1-bookworm-slim

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "7bb8605d08c5d3d42c6865f5e0bffaf25969c1fdedb3b8d2f6c4cfe68ade92a0  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.423 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.410 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
