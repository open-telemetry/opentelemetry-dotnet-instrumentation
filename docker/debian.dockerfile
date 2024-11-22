FROM mcr.microsoft.com/dotnet/sdk:9.0.100-bookworm-slim@sha256:7d24e90a392e88eb56093e4eb325ff883ad609382a55d42f17fd557b997022ca

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
