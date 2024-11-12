FROM mcr.microsoft.com/dotnet/sdk:8.0.403-bookworm-slim@sha256:b38da1961b1358940c634560747e09ef8047234e66c71033f19ac2e777f60240

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
