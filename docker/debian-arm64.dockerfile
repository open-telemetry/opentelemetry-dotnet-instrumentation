FROM mcr.microsoft.com/dotnet/sdk:9.0.101-bookworm-slim@sha256:fe8ceeca5ee197deba95419e3b85c32744970b730ae11645e13f1cb74a848d98

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "c169af55281cd1e58cdbe3ec95c2480cfb210ee460b3ff1421745c8f3236b263  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.404 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
