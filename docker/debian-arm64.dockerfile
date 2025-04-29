FROM mcr.microsoft.com/dotnet/sdk:9.0.203-bookworm-slim@sha256:fe3c1ed472bb0964c100f06aa9b1759f5ed84e0dfe6904d60f6a82159d3c7ae4

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "19b0a7890c371201b944bf0f8cdbb6460d053d63ddbea18cfed3e4199769ce17  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.408 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
