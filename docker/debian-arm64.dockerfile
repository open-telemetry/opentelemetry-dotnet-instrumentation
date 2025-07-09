FROM mcr.microsoft.com/dotnet/sdk:9.0.302-bookworm-slim@sha256:3da7c4198dc77b50aeaf76f262ed0ac2ed324f87fba4b5b0f0bc0b4fbbf2ad93

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.412 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
