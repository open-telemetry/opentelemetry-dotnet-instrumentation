FROM mcr.microsoft.com/dotnet/sdk:8.0.400-bookworm-slim

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-6.0 \
        dotnet-sdk-7.0 \
        cmake \
        clang \
        make

WORKDIR /project
