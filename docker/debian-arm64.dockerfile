FROM mcr.microsoft.com/dotnet/sdk:9.0.301-bookworm-slim@sha256:faa2daf2b72cbe787ee1882d9651fa4ef3e938ee56792b8324516f5a448f3abe

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL --retry 5 https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA512: $(sha512sum dotnet-install.sh)" \
    && echo "f8c59166ed912d6861e93c3efc2840be31ec32897679678a72f781423ebf061348d3b92b16c9541f5b312a34160f452826bb3021efb1414d76bd7e237e4c0e9a  dotnet-install.sh" | sha512sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.411 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
