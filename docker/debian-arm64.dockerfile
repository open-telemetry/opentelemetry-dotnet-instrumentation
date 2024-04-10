FROM mcr.microsoft.com/dotnet/sdk:8.0.204-bookworm-slim

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "170a3ec239a351f8d7c14bec424b286bd9468f4d928bdb7600f6424ea7f13927  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.421 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.408 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
