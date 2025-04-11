FROM mcr.microsoft.com/dotnet/sdk:9.0.203-bookworm-slim@sha256:9b0a4330cb3dac23ebd6df76ab4211ec5903907ad2c1ccde16a010bf25f8dfde

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
