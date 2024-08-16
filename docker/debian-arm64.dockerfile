FROM mcr.microsoft.com/dotnet/sdk:8.0.401-bookworm-slim

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "fe864d126da4d20b353e3983f186894a63009ce6fbe3108b0dac35a346331808  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.424 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.410 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
