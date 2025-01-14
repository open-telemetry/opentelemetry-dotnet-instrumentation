FROM mcr.microsoft.com/dotnet/sdk:9.0.101-bookworm-slim@sha256:26d070b5f4f32fbf3223f24cd27c5aef94f9074e54270eca2239b833563f6221

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
