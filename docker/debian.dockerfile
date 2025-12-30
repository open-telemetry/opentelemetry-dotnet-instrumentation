FROM mcr.microsoft.com/dotnet/sdk:9.0.308-bookworm-slim@sha256:51060ced0e5dc1282a587a092783132ba8b0ec25150d3bc61e99bf0ef2d62593
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
