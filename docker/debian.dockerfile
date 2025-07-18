FROM mcr.microsoft.com/dotnet/sdk:9.0.303-bookworm-slim@sha256:670ef9e8eca44c8baa0bd1c229ccde9537064260ef14d54738b7a87916609312

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
