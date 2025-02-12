FROM mcr.microsoft.com/dotnet/sdk:9.0.200-bookworm-slim@sha256:7f8e8b1514a2eeccb025f1e9dd554e191b21afa7f43f8321b7bd2009cdd59a1d

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
