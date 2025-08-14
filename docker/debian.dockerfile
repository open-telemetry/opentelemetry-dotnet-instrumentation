FROM mcr.microsoft.com/dotnet/sdk:9.0.304-bookworm-slim@sha256:ae000be75dac94fc40e00f0eee903289e985995cc06dac3937469254ce5b60b6

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
