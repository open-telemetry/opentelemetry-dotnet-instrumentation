FROM mcr.microsoft.com/dotnet/sdk:9.0.302-bookworm-slim@sha256:3da7c4198dc77b50aeaf76f262ed0ac2ed324f87fba4b5b0f0bc0b4fbbf2ad93

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-8.0 \
        cmake \
        clang \
        make

WORKDIR /project
