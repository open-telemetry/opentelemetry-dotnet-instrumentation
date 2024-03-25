FROM mcr.microsoft.com/dotnet/sdk:8.0.203-bookworm-slim

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "5eb82d8578f55cdadcb2edfd35ec649a2c6fc11a682e876b1cd68077badbf794  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.420 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.407 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
