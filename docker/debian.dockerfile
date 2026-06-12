FROM mcr.microsoft.com/dotnet/sdk:9.0.315-bookworm-slim@sha256:9f6264a75f6a36d090fbed4d6f9280f6b7abc4e4cdefeea7fcb1b612b2d90af4
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via apt-get

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-10.0 \
        dotnet-sdk-8.0 \
        cmake \
        clang \
        make

WORKDIR /project
