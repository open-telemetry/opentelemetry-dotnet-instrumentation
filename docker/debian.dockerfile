FROM mcr.microsoft.com/dotnet/sdk:9.0.310-bookworm-slim@sha256:0d84f05256dec37a5d1739158fd5ea197b8ad3b4e8d0e32be47b754db5963a9e
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
