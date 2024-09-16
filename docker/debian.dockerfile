FROM mcr.microsoft.com/dotnet/sdk:8.0.401-1-bookworm-slim@sha256:a364676fedc145cf88caad4bfb3cc372aae41e596c54e8a63900a2a1c8e364c6

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
