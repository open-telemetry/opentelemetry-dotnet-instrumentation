FROM mcr.microsoft.com/dotnet/sdk:9.0.202-bookworm-slim@sha256:d7f4691d11f610d9b94bb75517c9e78ac5799447b5b3e82af9e4625d8c8d1d53

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
    && ./dotnet-install.sh -v 8.0.407 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
