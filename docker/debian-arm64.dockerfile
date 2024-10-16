FROM mcr.microsoft.com/dotnet/sdk:8.0.403-bookworm-slim

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "de4957e41252191427a8ba0866f640b9f19c98fad62305919de41bd332e9c820  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.427 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.410 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
