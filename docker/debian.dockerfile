FROM mcr.microsoft.com/dotnet/sdk:9.0.304-bookworm-slim@sha256:840f3b62b9742dde4461a3c31e38ffd34d41d7d33afd39c378cfcfd5dcb82bd5

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
