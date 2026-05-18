FROM mcr.microsoft.com/dotnet/sdk:9.0.314-bookworm-slim@sha256:087fc98e5c6ffcea6c3e276c135c4a6717c589d9509a09cc22e7c634830a4db8
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
