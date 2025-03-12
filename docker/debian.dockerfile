FROM mcr.microsoft.com/dotnet/sdk:9.0.201-bookworm-slim@sha256:4845ef954a33b55c1a1f5db1ac24ba6cedb1dafb7f0b6a64ebce2fabe611f0c0

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
